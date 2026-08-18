namespace PlexSleepGuard.Plex;

public sealed record PlexPollResult(bool Success, IReadOnlyList<PlexSession> Sessions, string? Error)
{
    public static PlexPollResult Failed(string error) => new(false, Array.Empty<PlexSession>(), error);

    public static PlexPollResult Succeeded(IReadOnlyList<PlexSession> sessions) => new(true, sessions, null);
}
