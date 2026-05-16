using System;
using System.Diagnostics;

namespace exui_wpf;

public static class AppLogger
{
    public static event Action<string>? OnLog;

    public static void Log(string message)
    {
        var frame = new StackFrame(1);
        var type = frame.GetMethod()?.DeclaringType;
        
        if (type != null && type.Name.Contains("<"))
        {
            type = type.DeclaringType ?? type;
        }

        var callingClass = type?.Name ?? "Unknown";
        OnLog?.Invoke($"[{callingClass}] {message}");
    }
}