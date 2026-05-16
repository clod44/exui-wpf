using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace exui_wpf;

public class TelemetrySource : INotifyPropertyChanged
{
    private readonly ConcurrentDictionary<string, object> _data = new();
    public event PropertyChangedEventHandler? PropertyChanged;

    public TelemetrySource()
    {
        _data["connected"] = false;
    }

    public object this[string key]
    {
        get => _data.TryGetValue(key, out var val) ? val : 0;
        set
        {
            _data[key] = value;

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Entries)));
            });
        }
    }

    public List<KeyValuePair<string, object>> Entries => _data.ToList();
}