# Wizrod

Wizrod is a native Windows clipboard history MVP. Copy text normally, then press **Ctrl + Alt + V** to open its floating glass-style palette. Search matches instantly, recents are shown by default, and settings controls retention and the favourites view.

## Run

```powershell
dotnet run
```

The app stays in memory with no taskbar window. Copy text from any app, focus the destination, press `Ctrl + Alt + V`, then select a card to paste it. This first version intentionally stores text only and keeps history in memory; image/file capture and persistent encrypted history are the next production steps.
