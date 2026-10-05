// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_through_definitions;

public class different_guarded_stream_ids_for_the_same_source : given.a_definition_backed_batch
{
    async Task Because() => await Append([Item("2025-01"), Item("2025-02")]);

    [Fact] void should_fail_with_incompatible_scopes() => _exception.ShouldBeOfExactType<IncompatibleConcurrencyScopesForEventSource>();
    [Fact] void should_name_the_event_source_id() => ((IncompatibleConcurrencyScopesForEventSource)_exception).EventSourceId.ShouldEqual(_sourceId);
    [Fact] void should_report_both_guards() => ((IncompatibleConcurrencyScopesForEventSource)_exception).Scopes.Count.ShouldEqual(2);
    [Fact] void should_explain_the_way_out() => _exception.Message.ShouldContain("explicit concurrency scope");
    [Fact] void should_not_write_anything() => _sequences.DidNotReceiveWithAnyArgs().AppendManyForEventSources(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesRequest>(), Arg.Any<CallContext>());
}
