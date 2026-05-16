using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace exui_wpf;

public partial class Main : Window
{
    private static readonly string SettingsFile = Path.Combine("templates", "Main", "active_templates.conf");
    public ObservableCollection<TemplateItem> AvailableTemplates { get; } = new();
    
    // This is our globally shared state instance
    public static MainState State { get; } = new MainState();

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    public Main()
    {
        InitializeComponent();
        DiscoverProjectTemplates();
        TemplateListBox.ItemsSource = AvailableTemplates;
        
        ProcessBootConfiguration();

        this.Closed += (s, e) => Environment.Exit(0);
    }

    private void DiscoverProjectTemplates()
    {
        var windowTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => typeof(Window).IsAssignableFrom(t) && !t.IsAbstract && t != typeof(Window) && t.Name != "Main");

        foreach (var type in windowTypes)
        {
            AvailableTemplates.Add(new TemplateItem { Name = type.Name, Type = type });
        }
    }

    private void ProcessBootConfiguration()
    {
        if (!File.Exists(SettingsFile))
        {
            try
            {
                string? directory = Path.GetDirectoryName(SettingsFile);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                string defaultContent = "# exui Boot Configuration\nSpeedometer\n";
                File.WriteAllText(SettingsFile, defaultContent);
            }
            catch {}
            return;
        }

        try
        {
            string[] lines = File.ReadAllLines(SettingsFile);
            foreach (string line in lines)
            {
                string cleanName = line.Trim();
                if (string.IsNullOrWhiteSpace(cleanName) || cleanName.StartsWith("#")) continue;

                TemplateItem? match = AvailableTemplates.FirstOrDefault(t => t.Name.Equals(cleanName, StringComparison.OrdinalIgnoreCase));
                if (match != null) ExecuteLaunch(match);
            }
        }
        catch {}
    }

   private void ExecuteLaunch(TemplateItem item)
    {
        if (item.ActiveInstance != null) return;

        Window window = (Window)Activator.CreateInstance(item.Type)!;
        window.DataContext = State;
        
        window.MouseLeftButtonDown += (s, me) =>
        {
            if (State.DesignMode && me.LeftButton == MouseButtonState.Pressed)
            {
                window.DragMove();
            }
        };

        // FIX: Prevents Windows Aero Snap from hijacking the window and dropping it down at the top edge
        window.StateChanged += (s, ev) =>
        {
            if (window.WindowState == WindowState.Maximized)
            {
                window.WindowState = WindowState.Normal;
            }
        };

        window.SourceInitialized += (s, ev) => ApplyWindowInputStyle(window);
        window.Closed += (s, ev) => Application.Current.Dispatcher.Invoke(() => item.ActiveInstance = null);
        window.Show();
        item.ActiveInstance = window;
    }

    private void ApplyWindowInputStyle(Window window)
    {
        IntPtr hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;

        int extendedStyle = GetWindowLong(hwnd, -20);
        bool isGhost = window.Tag?.ToString() == "Ghost";

        if (!State.DesignMode && isGhost)
            SetWindowLong(hwnd, -20, extendedStyle | 0x00000020 | 0x00080000);
        else
            SetWindowLong(hwnd, -20, extendedStyle & ~0x00000020);
    }

    private void SynchronizeAllWindowStyles()
    {
        foreach (var item in AvailableTemplates)
        {
            if (item.ActiveInstance != null) ApplyWindowInputStyle(item.ActiveInstance);
        }
    }

    private void DesignMode_Checked(object sender, RoutedEventArgs e)
    {
        State.DesignMode = true;
        SynchronizeAllWindowStyles();
    }

    private void DesignMode_Unchecked(object sender, RoutedEventArgs e)
    {
        State.DesignMode = false;
        SynchronizeAllWindowStyles();
    }

    private void Launch_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.DataContext is TemplateItem item) 
        {
            ExecuteLaunch(item);
        }
    }

    private void Kill_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.DataContext is TemplateItem item && item.ActiveInstance != null) 
        {
            item.ActiveInstance.Close();
        }
}
}

public class TemplateItem : INotifyPropertyChanged
{
    private Window? _activeInstance;
    public string Name { get; set; } = string.Empty;
    public Type Type { get; set; } = null!;
    public Window? ActiveInstance { get => _activeInstance; set { _activeInstance = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsActive)); } }
    public bool IsActive => ActiveInstance != null;
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}