using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;

namespace Wizrod;

public sealed class ClipboardService : IDisposable
{
    private const int WmClipboardUpdate = 0x031D;
    private readonly HwndSource _source;
    private readonly List<ClipboardItem> _items = [];
    private readonly string _storagePath;
    public ReadOnlyCollection<ClipboardItem> Items => _items.AsReadOnly();
    public int RetentionDays { get; set; } = 14;
    public int FavoriteRetentionDays { get; set; } = 90;
    public bool FavoritesEnabled { get; set; } = true;
    public event Action? Changed;

    public ClipboardService()
    {
        _storagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Wizrod", "history.json");
        LoadHistory();
        var parameters = new HwndSourceParameters("WizrodClipboardListener") { Width = 0, Height = 0, WindowStyle = 0 };
        _source = new HwndSource(parameters);
        _source.AddHook(WndProc);
    }

    public void Start()
    {
        AddClipboardFormatListener(_source.Handle);
        ClearExpired();
        Application.Current.Dispatcher.BeginInvoke(CaptureText);
    }
    public bool Paste(ClipboardItem item, IntPtr destination, IntPtr focusedControl)
    {
        if (!TrySetClipboardText(item.Text)) return false;
        if (focusedControl != IntPtr.Zero)
        {
            SendMessage(focusedControl, 0x0302, IntPtr.Zero, IntPtr.Zero);
            return true;
        }
        if (destination != IntPtr.Zero)
        {
            RestoreDestination(destination);
            Thread.Sleep(100);
        }
        KeyboardPaste();
        return true;
    }
    public void ToggleFavorite(ClipboardItem item)
    {
        var index = _items.FindIndex(x => x.Id == item.Id);
        if (index < 0) return;
        var isFavorite = !item.IsFavorite;
        _items[index] = item with { IsFavorite = isFavorite, FavoritedAt = isFavorite ? DateTimeOffset.Now : null };
        SaveHistory();
        Changed?.Invoke();
    }
    public void ClearExpired()
    {
        var cutoff = DateTimeOffset.Now.AddDays(-RetentionDays);
        var favoriteCutoff = DateTimeOffset.Now.AddDays(-FavoriteRetentionDays);
        _items.RemoveAll(x => x.IsFavorite
            ? FavoriteRetentionDays != int.MaxValue && (x.FavoritedAt ?? x.CapturedAt) < favoriteCutoff
            : x.CapturedAt < cutoff);
        SaveHistory();
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
        var input = new[]
        {
            Input.KeyDown(0x11), Input.KeyDown(0x56), Input.KeyUp(0x56), Input.KeyUp(0x11)
        };
        SendInput((uint)input.Length, input, Marshal.SizeOf<Input>());
    }
    private static bool TrySetClipboardText(string text)
    {
        for (var attempt = 0; attempt < 6; attempt++)
        {
            try { Clipboard.SetText(text); return true; }
            catch (COMException) { Thread.Sleep(35); }
        }
        return false;
    }
    private void LoadHistory()
    {
        try
        {
            if (!File.Exists(_storagePath)) return;
            var history = JsonSerializer.Deserialize<List<ClipboardItem>>(File.ReadAllText(_storagePath));
            if (history is not null) _items.AddRange(history.OrderByDescending(x => x.CapturedAt).Take(250));
        }
        catch (JsonException) { }
        catch (IOException) { }
    }
    private void SaveHistory()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_storagePath)!);
            File.WriteAllText(_storagePath, JsonSerializer.Serialize(_items));
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
    private static void RestoreDestination(IntPtr destination)
    {
        var currentThread = GetCurrentThreadId();
        var destinationThread = GetWindowThreadProcessId(destination, IntPtr.Zero);
        var attached = destinationThread != 0 && AttachThreadInput(currentThread, destinationThread, true);
        try
        {
            BringWindowToTop(destination);
            SetForegroundWindow(destination);
            SetActiveWindow(destination);
            SetFocus(destination);
        }
        finally
        {
            if (attached) AttachThreadInput(currentThread, destinationThread, false);
        }
    }
    public void Dispose() { RemoveClipboardFormatListener(_source.Handle); _source.Dispose(); }
    [DllImport("user32.dll")] private static extern bool AddClipboardFormatListener(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool BringWindowToTop(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern IntPtr SetActiveWindow(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern IntPtr SetFocus(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd, IntPtr processId);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] private static extern bool AttachThreadInput(uint firstThread, uint secondThread, bool attach);
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, Input[] inputs, int size);
    [StructLayout(LayoutKind.Sequential)] private struct Input
    {
        public uint Type;
        public InputUnion Data;
        public static Input KeyDown(ushort key) => new() { Type = 1, Data = new InputUnion { Keyboard = new KeyboardInput { VirtualKey = key } } };
        public static Input KeyUp(ushort key) => new() { Type = 1, Data = new InputUnion { Keyboard = new KeyboardInput { VirtualKey = key, Flags = 2 } } };
    }
    [StructLayout(LayoutKind.Explicit)] private struct InputUnion
    {
        [FieldOffset(0)] public KeyboardInput Keyboard;
    }
    [StructLayout(LayoutKind.Sequential)] private struct KeyboardInput
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }
}
