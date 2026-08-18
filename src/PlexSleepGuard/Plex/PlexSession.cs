namespace PlexSleepGuard.Plex;

public sealed record PlexSession(string Title, string Type, string State, string SessionKey)
{
    public bool IsActive => !string.Equals(State, "stopped", StringComparison.OrdinalIgnoreCase);
}
