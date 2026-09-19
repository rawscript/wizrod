using System.Windows;

namespace Wizrod;

public partial class App : Application
{
    private ClipboardService? _clipboard;
    private HotkeyWindow? _window;
    private Mutex? _singleInstance;
    private bool _ownsMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _singleInstance = new Mutex(true, "Local\\Wizrod.SingleInstance", out var createdNew);
        _ownsMutex = createdNew;
        if (!createdNew)
        {
            MessageBox.Show("Wizrod is already running. Close the existing instance before starting another one.", "Wizrod", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }
        StartupRegistration.EnsureRegistered();
        _clipboard = new ClipboardService();
        _clipboard.Start();
        _window = new HotkeyWindow(_clipboard);
        _window.ShowRequested += () => _window.ShowAtCursor();
        _window.Closed += (_, _) => Shutdown();
        _window.Show();
        _window.Hide();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _clipboard?.Dispose();
        if (_ownsMutex) _singleInstance?.ReleaseMutex();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
