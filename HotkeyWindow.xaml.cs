using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Wizrod;

public partial class HotkeyWindow : Window
{
    private const int WmHotkey = 0x0312;
    private const int HotkeyId = 734;
    private readonly ClipboardService _clipboard;
    private bool _favoritesOnly;
    public event Action? ShowRequested;

    public HotkeyWindow(ClipboardService clipboard)
    {
        InitializeComponent();
        _clipboard = clipboard;
        _clipboard.Changed += RefreshItems;
        SourceInitialized += (_, _) =>
        {
            var source = (HwndSource)PresentationSource.FromVisual(this);
            source.AddHook(WndProc);
            RegisterHotKey(source.Handle, HotkeyId, 0x0001 | 0x0002, 0x56); // Alt + Ctrl + V
        };
        Closed += (_, _) => { var h = new WindowInteropHelper(this).Handle; if (h != IntPtr.Zero) UnregisterHotKey(h, HotkeyId); };
        RefreshItems();
    }

    public void ShowAtCursor()
    {
        _favoritesOnly = false; SearchPanel.Visibility = Visibility.Collapsed; SettingsPanel.Visibility = Visibility.Collapsed; SearchBox.Clear();
        RefreshItems();
        GetCursorPos(out var p);
        Left = Math.Max(12, Math.Min(p.X - Width / 2, SystemParameters.WorkArea.Right - Width - 12));
        Top = Math.Max(12, Math.Min(p.Y - 90, SystemParameters.WorkArea.Bottom - Height - 12));
        Show(); Activate(); Focus();
    }
    private void RefreshItems()
    {
        if (!IsLoaded) return;
        var query = SearchBox?.Text?.Trim() ?? "";
        IEnumerable<ClipboardItem> entries = _clipboard.Items;
        if (_favoritesOnly) entries = entries.Where(x => x.IsFavorite);
        if (!string.IsNullOrWhiteSpace(query)) entries = entries.Where(x => x.Text.Contains(query, StringComparison.OrdinalIgnoreCase));
        ItemsList.ItemsSource = entries.ToList();
    }
    private IntPtr WndProc(IntPtr h, int message, IntPtr w, IntPtr l, ref bool handled)
    {
        if (message == WmHotkey && w.ToInt32() == HotkeyId) { ShowRequested?.Invoke(); handled = true; }
        return IntPtr.Zero;
    }
    private void ItemsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ItemsList.SelectedItem is ClipboardItem item) { Hide(); _clipboard.Paste(item); ItemsList.SelectedItem = null; }
    }
    private void Item_Favorite(object sender, MouseButtonEventArgs e)
    {
        if (((System.Windows.Controls.ListBoxItem)sender).DataContext is ClipboardItem item)
            _clipboard.ToggleFavorite(item);
    }
    private void Search_Click(object sender, RoutedEventArgs e) { SettingsPanel.Visibility = Visibility.Collapsed; SearchPanel.Visibility = Visibility.Visible; SearchBox.Focus(); }
    private void Recents_Click(object sender, RoutedEventArgs e) { _favoritesOnly = false; SettingsPanel.Visibility = Visibility.Collapsed; RefreshItems(); }
    private void Favorites_Click(object sender, RoutedEventArgs e) { _favoritesOnly = true; SettingsPanel.Visibility = Visibility.Collapsed; RefreshItems(); }
    private void Settings_Click(object sender, RoutedEventArgs e) => SettingsPanel.Visibility = SettingsPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
    private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => RefreshItems();
    private void RetentionPicker_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_clipboard is null || RetentionPicker.SelectedIndex < 0) return;
        _clipboard.RetentionDays = RetentionPicker.SelectedIndex switch { 0 => 1, 1 => 7, 2 => 14, 3 => 30, _ => int.MaxValue };
        _clipboard.ClearExpired();
    }
    private void FavoritesToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (_clipboard is null) return;
        _clipboard.FavoritesEnabled = FavoritesToggle.IsChecked == true;
        FavoritesColumn.Width = _clipboard.FavoritesEnabled ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        FavoritesButton.Visibility = _clipboard.FavoritesEnabled ? Visibility.Visible : Visibility.Collapsed;
    }
    private void Window_Deactivated(object sender, EventArgs e) { if (IsVisible) Hide(); }
    private void Window_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Escape) Hide(); }
    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hwnd, int id);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out Point point);
    [StructLayout(LayoutKind.Sequential)] private struct Point { public int X; public int Y; }
}
