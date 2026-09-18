namespace Wizrod;

public sealed record ClipboardItem(Guid Id, string Text, DateTimeOffset CapturedAt, bool IsFavorite = false, DateTimeOffset? FavoritedAt = null)
{
    public string Preview => Text.Replace("\r", " ").Replace("\n", " ").Trim() is { Length: > 86 } t ? t[..86] + "…" : Text.Replace("\r", " ").Replace("\n", " ").Trim();
    public string TimeLabel => CapturedAt.LocalDateTime.ToString("h:mm tt");
}
