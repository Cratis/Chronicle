// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_a_job_reports_back.with_the_current_epoch;

public class from_an_observer_replay : given.an_observer_holding_a_newer_claim
{
    Task Establish() => HoldObserverReplay();

    Task Because() => _observer.ReplayedSuccessfullySince(44UL, new Dictionary<Key, EventSequenceNumber>(), [], DateTimeOffset.UtcNow, CurrentEpoch);

    [Fact] void should_end_the_replay() => _stateStorage.State.IsReplaying.ShouldBeFalse();
    [Fact] void should_release_the_claim() => _stateStorage.State.ObserverWideClaim.ShouldBeNull();
}
