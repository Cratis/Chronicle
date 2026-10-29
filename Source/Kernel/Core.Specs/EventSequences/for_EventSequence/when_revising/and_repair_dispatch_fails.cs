// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_revising;

/// <summary>
/// A full replay refusal propagates without attempting unsafe partition repair.
/// </summary>
public class and_repair_dispatch_fails : given.a_content_mutation_with_observers
{
    readonly Exception _failure = new GlobalProjectionReplayNotSupported();
    Exception _error;

    void Establish()
    {
        _definitions.RemoveAll(definition => definition.Identifier.Value != "arbitrary");
        _storedProjectionDefinitions["arbitrary"] = Definition("arbitrary") with { Scope = ProjectionScope.Global };
        _observers["arbitrary"].Replay().Returns(Task.FromException<JobId>(_failure));
    }

    async Task Because() => _error = await Catch.Exception(() => _eventSequence.Revise(42UL, _eventType, new JsonObject(), CorrelationId.New(), [], Identity.System));

    [Fact] void should_propagate_the_full_replay_refusal() => _error.ShouldEqual(_failure);
    [Fact] Task should_request_full_replay_once() => _observers["arbitrary"].Received(1).Replay();
    [Fact] Task should_not_fall_back_to_partition_replay() => _observers["arbitrary"].DidNotReceive().ReplayPartition(Arg.Any<Key>());
}
