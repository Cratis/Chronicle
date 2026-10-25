// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_rebuild_is_held;

public class and_an_unrelated_event_arrives_before_the_catch_up_completes : given.a_held_constraint_index_rebuild
{
    bool _refusingUnique;

    async Task Establish() => await HoldRebuild();

    async Task Because()
    {
        await PassLivenessCheck();
        _refusingUnique = _snapshots.Written!.ConstraintIndexCatchUpRefusing?.Contains(_uniqueDefinition.Name) == true;
        _result = await _eventSequence.Append(
            EventSourceType.Default,
            _eventSourceId,
            EventStreamType.All,
            EventStreamId.Default,
            new("Unconstrained", EventTypeGeneration.First),
            new(),
            CorrelationId.New(),
            [],
            Identity.System,
            [],
            ConcurrencyScope.None);
    }

    [Fact] void should_refuse_only_the_recovering_unique_constraint() => _refusingUnique.ShouldBeTrue();
    [Fact] void should_admit_the_unrelated_event() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_store_the_unrelated_event() => _log.Events.Count.ShouldEqual(3);
}
