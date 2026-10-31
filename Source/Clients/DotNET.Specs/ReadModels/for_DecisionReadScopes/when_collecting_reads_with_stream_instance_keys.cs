// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.ReadModels.for_DecisionReadScopes;

public class when_collecting_reads_with_stream_instance_keys : Specification
{
    IDecisionRead[] _reads;
    Dictionary<EventSourceId, ConcurrencyScope> _result;

    void Establish() => _reads =
    [
        new DecisionRead<Model>("first", null, "store", "namespace", (EventSequenceNumber)8, [new EventType("created", EventTypeGeneration.First)], new("source", "type", "first", null)),
        new DecisionRead<Model>("second", null, "store", "namespace", (EventSequenceNumber)4, [new EventType("created", EventTypeGeneration.First)], new("source", "type", "second", null))
    ];

    void Because() => _result = DecisionReadScopes.Collect(_reads, "store", "namespace");

    [Fact] void should_collect_one_source_scope() => _result.Keys.ShouldContainOnly((EventSourceId)"source");
    [Fact] void should_widen_to_all_streams() => _result[(EventSourceId)"source"].EventStreamId.ShouldBeNull();

    record Model(string Id);
}
