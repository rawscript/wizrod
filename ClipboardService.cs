using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Wizrod;

public sealed class ClipboardService : IDisposable
{
    private const int WmClipboardUpdate = 0x031D;
    private readonly HwndSource _source;
    private readonly List<ClipboardItem> _items = [];
    public ReadOnlyCollection<ClipboardItem> Items => _items.AsReadOnly();
    public int RetentionDays { get; set; } = 14;
    public bool FavoritesEnabled { get; set; } = true;
    public event Action? Changed;

    public ClipboardService()
    {
        var parameters = new HwndSourceParameters("WizrodClipboardListener") { Width = 0, Height = 0, WindowStyle = 0 };
        _source = new HwndSource(parameters);
        _source.AddHook(WndProc);
    }

    public void Start() => AddClipboardFormatListener(_source.Handle);
    public void Paste(ClipboardItem item)
    {
        Clipboard.SetText(item.Text);
        KeyboardPaste();
    }
    public void ToggleFavorite(ClipboardItem item)
    {
        var index = _items.FindIndex(x => x.Id == item.Id);
        if (index < 0) return;
        _items[index] = item with { IsFavorite = !item.IsFavorite };
        Changed?.Invoke();
    }
    public void ClearExpired()
    {
        var cutoff = DateTimeOffset.Now.AddDays(-RetentionDays);
        _items.RemoveAll(x => !x.IsFavorite && x.CapturedAt < cutoff);
        Changed?.Invoke();
    }
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WmClipboardUpdate) return IntPtr.Zero;
        Application.Current.Dispatcher.BeginInvoke(CaptureText);
        return IntPtr.Zero;
    }
    private void CaptureText()
    {
        try
        {
            if (!Clipboard.ContainsText()) return;
            var text = Clipboard.GetText().Trim();
            if (string.IsNullOrWhiteSpace(text) || _items.FirstOrDefault()?.Text == text) return;
            _items.Insert(0, new ClipboardItem(Guid.NewGuid(), text, DateTimeOffset.Now));
            if (_items.Count > 250) _items.RemoveRange(250, _items.Count - 250);
            ClearExpired();
        }
        catch (COMException) { }
    }
    private static void KeyboardPaste()
    {
        keybd_event(0x11, 0, 0, UIntPtr.Zero); keybd_event(0x56, 0, 0, UIntPtr.Zero);
        keybd_event(0x56, 0, 2, UIntPtr.Zero); keybd_event(0x11, 0, 2, UIntPtr.Zero);
    }
    public void Dispose() { RemoveClipboardFormatListener(_source.Handle); _source.Dispose(); }
    [DllImport("user32.dll")] private static extern bool AddClipboardFormatListener(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
}
