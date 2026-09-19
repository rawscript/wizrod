# Wizrod
![Wizrod Banner](https://res.cloudinary.com/dp7vwr0av/image/upload/v1789720358/Wizrod_ko5kwt.png)

Wizrod is a native Windows clipboard history MVP. Copy text normally, then press **Ctrl + Alt + V** to open its floating glass-style palette. Search matches instantly, recents are shown by default, and settings controls retention and the favourites view.

## Run from a build

After `dotnet build`, start the normal Windows executable directly:

```powershell
.\bin\Debug\net8.0-windows\Wizrod.exe
```

## Create a portable app

Create a self-contained executable that does not require the .NET SDK or a PowerShell window:

```powershell
dotnet publish -p:PublishProfile=PortableWindows
```

The resulting app is `publish\Wizrod.exe`. Copy it to a permanent location on any Windows 64-bit PC and open it once normally. Wizrod registers itself to start automatically for that Windows user after every sign-in. The app stays in memory with no taskbar window. Copy text from any app, focus the destination, press `Ctrl + Alt + V`, then select a card to paste it. This first version intentionally stores text only and keeps history in memory; image/file capture and persistent encrypted history are the next production steps.
