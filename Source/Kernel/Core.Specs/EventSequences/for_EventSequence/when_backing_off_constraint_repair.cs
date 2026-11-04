// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.EventSequences.for_EventSequence;

public class when_backing_off_constraint_repair : given.an_event_sequence
{
    readonly given.RepairClock _clock = new();
    readonly List<TimeSpan> _delays = [];
    double _sample;

    void Establish()
    {
        _eventSequence.ConstraintIndexTimeProvider = _clock;
        _eventSequence.ConstraintIndexRetryJitterSample = () => _sample;
    }

    void Because()
    {
        var backOff = typeof(EventSequence).GetMethod("BackOffConstraintRepair", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var failures = typeof(EventSequence).GetField("_constraintRepairFailures", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var next = typeof(EventSequence).GetField("_nextConstraintRepairAttempt", BindingFlags.Instance | BindingFlags.NonPublic)!;
        foreach (var (count, sample) in new[] { (0, 0d), (0, 1d), (3, 0d), (3, 1d), (20, 0d), (20, 1d) })
        {
            failures.SetValue(_eventSequence, count);
            _sample = sample;
            backOff.Invoke(_eventSequence, null);
            _delays.Add((DateTimeOffset)next.GetValue(_eventSequence)! - _clock.GetUtcNow());
        }
    }

    [Fact] void should_jitter_the_initial_delay_down_by_twenty_percent() => _delays[0].ShouldEqual(TimeSpan.FromSeconds(0.8));
    [Fact] void should_jitter_the_initial_delay_up_by_twenty_percent() => _delays[1].ShouldEqual(TimeSpan.FromSeconds(1.2));
    [Fact] void should_jitter_the_exponential_delay_down_by_twenty_percent() => _delays[2].ShouldEqual(TimeSpan.FromSeconds(6.4));
    [Fact] void should_jitter_the_exponential_delay_up_by_twenty_percent() => _delays[3].ShouldEqual(TimeSpan.FromSeconds(9.6));
    [Fact] void should_jitter_the_capped_delay_down_by_twenty_percent() => _delays[4].ShouldEqual(TimeSpan.FromSeconds(48));
    [Fact] void should_keep_the_sampled_delay_within_sixty_seconds() => _delays[5].ShouldEqual(TimeSpan.FromSeconds(60));
}
