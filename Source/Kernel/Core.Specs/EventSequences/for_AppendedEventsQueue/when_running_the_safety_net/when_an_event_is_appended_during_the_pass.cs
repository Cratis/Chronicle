// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_running_the_safety_net;

/// <summary>
/// An event appended between reading the cursors and reading the tails is part of the tail but not of the cursor, so the
/// observer is woken rather than taken for up to date.
/// </summary>
public class when_an_event_is_appended_during_the_pass : given.a_queue_with_a_stored_durable_subscription
{
    void Establish()
    {
        SetCursor(_observerKey.ObserverId, 11);
        _tails[_eventType.Id] = 10;
        _observerStates.GetNextEventSequenceNumbers(_eventSequence).Returns(_ =>
        {
            _lookups.Enqueue("cursors");
            var cursors = new Dictionary<ObserverId, EventSequenceNumber>(_cursors);

            // The append lands after the cursors were read.
            _tails[_eventType.Id] = 11;
            return Task.FromResult<IReadOnlyDictionary<ObserverId, EventSequenceNumber>>(cursors);
        });
    }

    async Task Because()
    {
        _clock.Advance(TimeSpan.FromSeconds(SafetyNetSeconds));
        await _queue.RunDormantSafetyNet();
    }

    [Fact] void should_run_one_watchdog_pass() => _calls.ToArray().ShouldEqual(["watchdog"]);
    [Fact] void should_have_read_the_cursors_before_the_tails() => _lookups.First().ShouldEqual("cursors");
}
