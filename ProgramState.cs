using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace exui_wpf;

public class ProgramState : INotifyPropertyChanged
{
    private bool _isEditorMode = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsEditorMode
    {
        get => _isEditorMode;
        set
        {
            if (_isEditorMode != value)
            {
                _isEditorMode = value;
                OnPropertyChanged();
            }
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}