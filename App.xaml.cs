using System.Windows;

namespace Wizrod;

public partial class App : Application
{
    private ClipboardService? _clipboard;
    private HotkeyWindow? _window;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _clipboard = new ClipboardService();
        _clipboard.Start();
        _window = new HotkeyWindow(_clipboard);
        _window.ShowRequested += () => _window.ShowAtCursor();
        _window.Closed += (_, _) => Shutdown();
        // Creating the native handle registers the global hotkey before the window is hidden.
        _window.Show();
        _window.Hide();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _clipboard?.Dispose();
        base.OnExit(e);
    }
}
