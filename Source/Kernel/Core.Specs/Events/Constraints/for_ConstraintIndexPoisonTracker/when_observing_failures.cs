// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.InMemory;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexPoisonTracker;

public class when_observing_failures : Specification
{
    readonly ConstraintIndexPoisonTracker _tracker = new(new InMemoryStorageErrorClassifier());

    [Fact] void should_require_five_identical_attempts() => Observe(4).ShouldBeEmpty();
    [Fact] void should_settle_the_fifth_attempt() => Observe(5).Keys.ShouldContain((ConstraintName)"unique");
    [Fact] void should_require_other_successful_storage_calls() => Observe(6, storageSucceeded: false).ShouldBeEmpty();
    [Fact] void should_not_settle_a_failure_without_an_event_position() => Observe(6, eventIdentified: false).ShouldBeEmpty();
    [Fact] void should_not_settle_cancellation() => Observe(6, error: new OperationCanceledException()).ShouldBeEmpty();
    [Fact] void should_not_settle_timeout() => Observe(6, error: new TimeoutException()).ShouldBeEmpty();

    [Fact]
    void should_restart_the_count_for_another_exception_type()
    {
        Observe(4);
        Observe(1, error: new InvalidOperationException());
        Observe(4).ShouldBeEmpty();
    }

    [Fact]
    void should_restart_the_count_for_another_event()
    {
        Observe(4);
        Observe(1, position: 2);
        Observe(4).ShouldBeEmpty();
    }

    [Fact]
    void should_restart_the_count_after_a_successful_attempt()
    {
        Observe(4);
        _tracker.Observe(new Dictionary<ConstraintName, ConstraintIndexReplayFailure>(), true);
        Observe(4).ShouldBeEmpty();
    }

    IReadOnlyDictionary<ConstraintName, ConstraintIndexReplayFailure> Observe(int attempts, EventSequenceNumber? position = null, Exception? error = null, bool storageSucceeded = true, bool eventIdentified = true)
    {
        IReadOnlyDictionary<ConstraintName, ConstraintIndexReplayFailure> result = new Dictionary<ConstraintName, ConstraintIndexReplayFailure>();
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            result = _tracker.Observe(new Dictionary<ConstraintName, ConstraintIndexReplayFailure> { ["unique"] = new(eventIdentified ? position ?? EventSequenceNumber.First : null, error ?? new FormatException()) }, storageSucceeded);
        }
        return result;
    }
}
