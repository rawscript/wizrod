using Microsoft.Win32;

namespace Wizrod;

internal static class StartupRegistration
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Wizrod";

    /// <summary>
    /// Registers the executable for the current user so that it starts after sign-in.
    /// </summary>
    public static void EnsureRegistered()
    {
        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executablePath)) return;

        try
        {
            using var runKey = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
            runKey?.SetValue(ValueName, $"\"{executablePath}\"", RegistryValueKind.String);
        }
        catch (UnauthorizedAccessException)
        {
            // Wizrod can still run normally when startup registration is unavailable.
        }
        catch (System.Security.SecurityException)
        {
            // Some managed devices prevent users from changing their startup entries.
        }
    }
}
