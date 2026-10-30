// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.for_Observer.when_restoring_live_subscription_after_zero_work;

public class and_the_gap_lookup_proves_absence : given.an_observer_rejoining_during_backoff
{
    async Task Because()
    {
        await CompletePendingJobs();
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(GetSequenceNumberError.NotFound);
        _queued = false;
        await _observer.CatchUp();
        await DeliverLive(30L);
    }

    [Fact] async Task should_release_the_gap_only_after_authoritative_absence() => (await _observer.GetState()).NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)31L);
    [Fact] void should_not_start_another_zero_key_job() => _preparedJobs.Count.ShouldEqual(3);
    [Fact] void should_restore_live_delivery() => _queued.ShouldBeTrue();
    [Fact] void should_not_need_the_clock_to_advance() => _clock.GetUtcNow().ShouldEqual(DateTimeOffset.UnixEpoch);
}
