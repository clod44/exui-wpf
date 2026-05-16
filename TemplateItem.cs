using System;
using System.ComponentModel;
using System.Windows;

namespace exui_wpf;

public class TemplateItem : INotifyPropertyChanged
{
    private bool _isActive;
    public string Name { get; set; } = string.Empty;
    public Type Type { get; set; } = null!;
    public Window? Instance { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsActive
    {
        get => _isActive;
        set
        {
            _isActive = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsActive)));
        }
    }
}