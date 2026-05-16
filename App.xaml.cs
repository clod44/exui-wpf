using System;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace exui_wpf;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Console.CancelKeyPress += (s, ev) => Environment.Exit(0);

        ExuiClient.Start();

        Type? mainTemplateType = Assembly.GetExecutingAssembly().GetTypes()
            .FirstOrDefault(t => typeof(Window).IsAssignableFrom(t) && !t.IsAbstract && t.Name == "Main");

        if (mainTemplateType != null)
        {
            Window mainWindow = (Window)Activator.CreateInstance(mainTemplateType)!;
            mainWindow.Show();
        }
        else
        {
            MessageBox.Show("Critical Error: Could not find the 'Main' template folder or window inside your project workspace.", "exui engine error", MessageBoxButton.OK, MessageBoxImage.Error);
            Environment.Exit(0);
        }
    }
}