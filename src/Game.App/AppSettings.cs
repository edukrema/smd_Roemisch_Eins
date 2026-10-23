namespace Game.App;

/// <summary>
/// Central app configuration. For the Android emulator the Api port is forwarded to
/// localhost via "adb reverse" (see README).
/// </summary>
public static class AppSettings
{
    public const string ApiBaseUrl = "http://localhost:5000";
}
