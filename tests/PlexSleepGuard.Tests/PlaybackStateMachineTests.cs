using PlexSleepGuard.State;

namespace PlexSleepGuard.Tests;

public sealed class PlaybackStateMachineTests
{
    private static readonly DateTimeOffset Start = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void IdleToPlayingWhenAnySessionIsActive()
    {
        var machine = new PlaybackStateMachine(TimeSpan.FromMinutes(15));
        var transition = machine.ObserveActive(true, Start);
        Assert.Equal(PlaybackState.Playing, machine.State);
        Assert.Equal((PlaybackState.Idle, PlaybackState.Playing), (transition!.From, transition.To));
    }

    [Fact]
    public void PlayingToGracePeriodWhenFinalSessionEnds()
    {
        var machine = PlayingMachine();
        var transition = machine.ObserveActive(false, Start.AddMinutes(1));
        Assert.Equal(PlaybackState.GracePeriod, machine.State);
        Assert.Equal(Start.AddMinutes(16), transition!.GraceEndsAt);
    }

    [Fact]
    public void GracePeriodExpiresToIdle()
    {
        var machine = PlayingMachine(TimeSpan.FromMinutes(5));
        machine.ObserveActive(false, Start.AddMinutes(1));
        var transition = machine.Advance(Start.AddMinutes(6));
        Assert.Equal(PlaybackState.Idle, machine.State);
        Assert.Equal((PlaybackState.GracePeriod, PlaybackState.Idle), (transition!.From, transition.To));
    }

    [Fact]
    public void PlaybackResumesDuringGrace()
    {
        var machine = PlayingMachine();
        machine.ObserveActive(false, Start.AddMinutes(1));
        var transition = machine.ObserveActive(true, Start.AddMinutes(2));
        Assert.Equal(PlaybackState.Playing, machine.State);
        Assert.Null(machine.GraceEndsAt);
        Assert.Equal((PlaybackState.GracePeriod, PlaybackState.Playing), (transition!.From, transition.To));
    }

    [Fact]
    public void OneOfMultipleSessionsEndingDoesNotStartGrace()
    {
        var machine = PlayingMachine();
        var transition = machine.ObserveActive(true, Start.AddMinutes(1));
        Assert.Null(transition);
        Assert.Equal(PlaybackState.Playing, machine.State);
    }

    [Fact]
    public void PausedSessionIsStillActiveByPolicy()
    {
        var machine = new PlaybackStateMachine(TimeSpan.FromMinutes(15));
        machine.ObserveActive(true, Start);
        var transition = machine.ObserveActive(true, Start.AddMinutes(30));
        Assert.Null(transition);
        Assert.Equal(PlaybackState.Playing, machine.State);
    }

    [Fact]
    public void ATransientPollFailureDoesNotLookLikePlaybackEnded()
    {
        var machine = PlayingMachine();
        var transition = machine.Advance(Start.AddMinutes(2));
        Assert.Null(transition);
        Assert.Equal(PlaybackState.Playing, machine.State);
    }

    private static PlaybackStateMachine PlayingMachine(TimeSpan? grace = null)
    {
        var machine = new PlaybackStateMachine(grace ?? TimeSpan.FromMinutes(15));
        machine.ObserveActive(true, Start);
        return machine;
    }
}
