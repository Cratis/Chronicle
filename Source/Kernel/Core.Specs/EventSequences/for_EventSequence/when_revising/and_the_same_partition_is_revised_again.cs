// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_revising;

public class and_the_same_partition_is_revised_again : given.a_content_mutation_with_observers
{
    readonly List<CorrelationId> _stored = [];
    readonly List<int> _storedCountsAtRepair = [];
    readonly CorrelationId _first = CorrelationId.New();
    readonly CorrelationId _second = CorrelationId.New();

    void Establish()
    {
        _eventSequenceStorage.WhenForAnyArgs(_ => _.Revise(default!, default!, default!, default!, default!, default, default!, default!))
            .Do(call => _stored.Add(call.Arg<CorrelationId>()));
        _observers["implicit"].When(_ => _.ReplayPartition((Key)_eventSourceId)).Do(_ => _storedCountsAtRepair.Add(_stored.Count));
    }

    async Task Because()
    {
        await _eventSequence.Revise(42UL, _eventType, new JsonObject { ["value"] = 1 }, _first, [], Identity.System);
        await _eventSequence.Revise(42UL, _eventType, new JsonObject { ["value"] = 2 }, _second, [], Identity.System);
    }

    [Fact] void should_store_both_revisions() => _stored.ShouldEqual(new[] { _first, _second });
    [Fact] void should_request_a_repair_after_each_revision_is_stored() => _storedCountsAtRepair.ShouldEqual(new[] { 1, 2 });
}
