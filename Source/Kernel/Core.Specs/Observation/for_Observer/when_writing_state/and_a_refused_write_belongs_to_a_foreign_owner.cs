// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_writing_state;

public class and_a_refused_write_belongs_to_a_foreign_owner : given.an_observer_with_controlled_persistence
{
    readonly EventSequenceId _otherSequence = new("other-sequence");
    ulong? _epoch;
    int _attemptsBefore;

    Task Because() => InGrainTurn(PerformScenario);

    async Task PerformScenario()
    {
        _attemptsBefore = _writes.Attempts;
        _writes.BeforeWrite = () =>
        {
            _definition.State = _definition.State with { EventSequenceId = _otherSequence };
            return Task.FromException(new ObserverKeyDoesNotMatchDefinition(_observerKey, _otherSequence));
        };
        _epoch = await _observer.GrantObserverWideBeforeStart(ClaimHolderKind.ObserverCatchUp, 0UL);
    }

    [Fact] void should_not_retry_the_refused_write() => _writes.Attempts.ShouldEqual(_attemptsBefore + 1);
    [Fact] void should_not_reclaim_the_foreign_record() => _eventStoreNamespaceStorage.Observers.DidNotReceive().ClaimOwnership(Arg.Any<ObserverId>(), Arg.Any<EventSequenceId>());
    [Fact] void should_not_report_a_persisted_grant() => _epoch.ShouldBeNull();
}
