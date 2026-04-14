using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ArcaPhylax.Abstractions;

public abstract class NotifyBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void Raise([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    protected bool Set<T>(ref T backing, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(backing, value))
        {
            return false;
        }

        backing = value;
        Raise(name);

        return true;
    }
}
