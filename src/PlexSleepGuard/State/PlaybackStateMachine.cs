namespace PlexSleepGuard.State;

public sealed class PlaybackStateMachine
{
    private readonly TimeSpan gracePeriod;
    private DateTimeOffset? graceEndsAt;

    public PlaybackStateMachine(TimeSpan gracePeriod)
    {
        if (gracePeriod < TimeSpan.Zero)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(gracePeriod, TimeSpan.Zero);
        }

        this.gracePeriod = gracePeriod;
    }

    public PlaybackState State { get; private set; } = PlaybackState.Idle;

    public DateTimeOffset? GraceEndsAt => graceEndsAt;

    public PlaybackTransition? ObserveActive(bool active, DateTimeOffset now)
    {
        return State switch
        {
            PlaybackState.Idle when active => TransitionTo(PlaybackState.Playing, now, null),
            PlaybackState.Playing when !active => StartGrace(now),
            PlaybackState.GracePeriod when active => TransitionTo(PlaybackState.Playing, now, null),
            _ => null
        };
    }

    public PlaybackTransition? Advance(DateTimeOffset now)
    {
        if (State == PlaybackState.GracePeriod && graceEndsAt is not null && now >= graceEndsAt.Value)
        {
            return TransitionTo(PlaybackState.Idle, now, graceEndsAt);
        }

        return null;
    }

    private PlaybackTransition StartGrace(DateTimeOffset now)
    {
        graceEndsAt = now + gracePeriod;
        return TransitionTo(PlaybackState.GracePeriod, now, graceEndsAt);
    }

    private PlaybackTransition TransitionTo(PlaybackState next, DateTimeOffset now, DateTimeOffset? end)
    {
        var transition = new PlaybackTransition(State, next, now, end);
        State = next;
        if (next != PlaybackState.GracePeriod)
        {
            graceEndsAt = null;
        }

        return transition;
    }
}
