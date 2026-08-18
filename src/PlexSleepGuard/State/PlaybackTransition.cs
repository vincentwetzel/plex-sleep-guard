namespace PlexSleepGuard.State;

public sealed record PlaybackTransition(
    PlaybackState From,
    PlaybackState To,
    DateTimeOffset At,
    DateTimeOffset? GraceEndsAt);
