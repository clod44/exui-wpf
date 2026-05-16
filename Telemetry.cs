using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace exui_wpf;

public class Telemetry : INotifyPropertyChanged
{
    private readonly ConcurrentDictionary<string, object> _data = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public object this[string key]
    {
        get => _data.TryGetValue(key, out var value) ? value : 0f;
        set
        {
            _data[key] = value;
            
            // CRITICAL: Tells WPF that indexer properties (e.g. Telemetry[speed]) changed
            OnPropertyChanged("Item[]");
            
            // Tells the Debug Matrix view to refresh
            OnPropertyChanged(nameof(AllEntries));
        }
    }

    public IReadOnlyDictionary<string, object> AllEntries => _data;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}