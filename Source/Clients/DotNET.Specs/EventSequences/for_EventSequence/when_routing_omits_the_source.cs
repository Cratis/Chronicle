// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;

namespace Cratis.Chronicle.EventSequences.for_EventSequence;

public class when_routing_omits_the_source : given.a_definition_backed_batch
{
    void Establish() => _eventSources.GetFor(typeof(EventSources.for_EventSources.ShoppingCartEventSource)).Returns(
        new EventSourceDefinition(
            typeof(EventSources.for_EventSources.ShoppingCartEventSource),
            "ShoppingCart",
            string.Empty,
            ConcurrencyDimensions.EventStreamType | ConcurrencyDimensions.EventStreamId,
            [new EventStream("Items", string.Empty, ConcurrencyDimensions.None)]));

    async Task Because() => await Append([Item("shared-stream")]);

    [Fact] void should_not_fail() => _exception.ShouldBeNull();
    [Fact] void should_read_the_tail_without_narrowing_by_source() => _tailSource.Received(1).GetTailSequenceNumber(null, null, new EventStreamType("Items"), new EventStreamId("shared-stream"), null);
    [Fact] void should_keep_the_source_id_as_the_scope_label() => _request.ConcurrencyScopes.Single().EventSourceId.ShouldEqual(_sourceId.Value);
    [Fact] void should_not_narrow_the_scope_by_source() => _request.ConcurrencyScopes.Single().Scope.EventSourceId.ShouldBeFalse();
    [Fact] void should_not_narrow_the_scope_by_source_type() => _request.ConcurrencyScopes.Single().Scope.EventSourceType.ShouldBeNull();
    [Fact] void should_guard_the_stream_type() => _request.ConcurrencyScopes.Single().Scope.EventStreamType.ShouldEqual("Items");
    [Fact] void should_guard_the_stream_id() => _request.ConcurrencyScopes.Single().Scope.EventStreamId.ShouldEqual("shared-stream");
    [Fact] void should_use_the_filtered_tail_as_the_expected_position() => _request.ConcurrencyScopes.Single().Scope.SequenceNumber.ShouldEqual(10UL);
    [Fact] void should_still_write_the_event_with_its_source_id() => _request.Events.Single().EventSourceId.ShouldEqual(_sourceId.Value);
    [Fact] void should_still_write_the_event_with_its_source_type() => _request.Events.Single().EventSourceType.ShouldEqual("ShoppingCart");
}
