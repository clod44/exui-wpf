using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace exui_wpf;

public class MainState : INotifyPropertyChanged
{
    private bool _designMode = true;

    public TelemetrySource Telemetry => ExuiClient.Telemetry;

    public bool DesignMode
    {
        get => _designMode;
        set
        {
            _designMode = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
}