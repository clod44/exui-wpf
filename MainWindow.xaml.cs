using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace exui_wpf;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly CancellationTokenSource _cts = new();
    private readonly TelemetryClient _client;
    private string _connectionStatus = "DISCONNECTED";
    private string _logText = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;
    public Telemetry Telemetry { get; } = new();
    public ProgramState ProgramState { get; } = new();
    public ObservableCollection<KeyValuePair<string, object>> TelemetryRows { get; } = new();
    public ObservableCollection<TemplateItem> Templates { get; } = new();

    public string ConnectionStatus
    {
        get => _connectionStatus;
        set
        {
            _connectionStatus = value;
            OnPropertyChanged(nameof(ConnectionStatus));
        }
    }

    public string LogText
    {
        get => _logText;
        set
        {
            _logText = value;
            OnPropertyChanged(nameof(LogText));
        }
    }

    public MainWindow()
    {
        InitializeComponent();
        this.DataContext = this;

        _client = new TelemetryClient(Telemetry);

        AppLogger.OnLog += HandleIncomingLog;
        Telemetry.PropertyChanged += OnTelemetryDataReceived;
        
        this.Loaded += OnWindowLoaded;
        this.Closed += OnWindowClosed;
    }

    private void HandleIncomingLog(string formattedMessage)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            LogText += $"[{DateTime.Now:HH:mm:ss}] {formattedMessage}{Environment.NewLine}";

            if (formattedMessage.Contains("connected successfully")) ConnectionStatus = "CONNECTED";
            else if (formattedMessage.Contains("Exception") || formattedMessage.Contains("closed")) ConnectionStatus = "DISCONNECTED";
        });
    }

    private void OnTelemetryDataReceived(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "AllEntries")
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                TelemetryRows.Clear();
                foreach (var kvp in Telemetry.AllEntries)
                {
                    TelemetryRows.Add(kvp);
                }
            });
        }
    }
    private void DiscoverPlugins()
    {
        string executionDir = AppDomain.CurrentDomain.BaseDirectory;
        string templatesFolder = Path.GetFullPath(Path.Combine(executionDir, "..", "templates"));
        
        AppLogger.Log($"Probing engine target directory: {templatesFolder}");

        if (!Directory.Exists(templatesFolder))
        {
            AppLogger.Log("Scanner idle: Target template directory does not exist.");
            return;
        }

        string[] dllFiles = Directory.GetFiles(templatesFolder, "*.dll", SearchOption.AllDirectories);
        
        // Track unique assembly names processed during this scan pass
        var loadedAssemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string dllPath in dllFiles)
        {
            string fileName = Path.GetFileName(dllPath);
            
            if (fileName.Equals("exui_wpf.dll", StringComparison.OrdinalIgnoreCase)) 
                continue;

            try
            {
                // Inspect the internal assembly identity without loading it into the runtime execution context
                AssemblyName assemblyName = AssemblyName.GetAssemblyName(dllPath);
                string asmNameKey = assemblyName.Name ?? string.Empty;

                if (string.IsNullOrEmpty(asmNameKey)) continue;

                // If a copy of this plugin was already discovered in another subfolder, skip it
                if (loadedAssemblyNames.Contains(asmNameKey))
                {
                    continue;
                }

                Assembly pluginAssembly = Assembly.LoadFrom(dllPath);
                var windowTypes = pluginAssembly.GetTypes()
                    .Where(t => typeof(Window).IsAssignableFrom(t) && !t.IsAbstract && t != typeof(Window));

                foreach (var type in windowTypes)
                {
                    if (Templates.Any(t => t.Type == type)) continue;

                    Templates.Add(new TemplateItem { Name = type.Name, Type = type });
                    AppLogger.Log($"Discovered template component: {type.Name}");
                }

                loadedAssemblyNames.Add(asmNameKey);
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Failed loading binary [{fileName}]: {ex.Message}");
            }
        }
    }
    private void OnTemplateActivationChanged(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.DataContext is TemplateItem item)
        {
            if (checkBox.IsChecked == true)
            {
                try
                {
                    if (item.Instance == null)
                    {
                        item.Instance = (Window)Activator.CreateInstance(item.Type)!;
                        item.Instance.DataContext = this;
                        
                        item.Instance.Closed += (s, args) =>
                        {
                            item.Instance = null;
                            item.IsActive = false;
                            AppLogger.Log($"Template window closed: {item.Name}");
                        };
                    }
                    
                    item.Instance.Show();
                    AppLogger.Log($"Activated template window: {item.Name}");
                }
                catch (Exception ex)
                {
                    checkBox.IsChecked = false;
                    AppLogger.Log($"Activation Crash [{item.Name}]: {ex.Message}");
                }
            }
            else
            {
                if (item.Instance != null)
                {
                    item.Instance.Close();
                    item.Instance = null;
                }
            }
        }
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        AppLogger.Log("Dashboard interface loaded.");
        DiscoverPlugins();
        Task.Run(() => _client.StartAsync(_cts.Token));
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        AppLogger.OnLog -= HandleIncomingLog;
        _cts.Cancel();
        _cts.Dispose();
        
        foreach (var item in Templates)
        {
            item.Instance?.Close();
        }
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}