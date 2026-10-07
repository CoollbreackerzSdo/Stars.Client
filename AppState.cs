namespace Stars.Client;

/// <summary>Shared view-model instance so every tab reflects the same live state.</summary>
public static class AppState
{
    public static MainViewModel Main { get; } = new();

    private static bool _introPlayed;

    /// <summary>Returns true exactly once per app launch, so entry animations run only the first time.</summary>
    public static bool ConsumeIntro()
    {
        if (_introPlayed)
            return false;

        _introPlayed = true;
        return true;
    }
}
