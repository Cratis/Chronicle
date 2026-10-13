// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.for_EventSequence;

/// <summary>
/// The routed events would require incompatible definition-derived guards without the concrete scope.
/// </summary>
public class when_a_concrete_scope_skips_definition_resolution : given.a_definition_backed_batch
{
    ConcurrencyScope _scope;

    void Establish() => _scope = new ConcurrencyScope(77UL, _sourceId);

    async Task Because() => await Append([Item("first-stream"), Item("second-stream")], new Dictionary<EventSourceId, ConcurrencyScope> { [_sourceId] = _scope });

    [Fact] void should_not_reject_the_incompatible_definition_guards() => _exception.ShouldBeNull();
    [Fact] void should_not_resolve_any_definition_guard() => _tailSource.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_send_only_the_concrete_scope() => _request.ConcurrencyScopes.Count().ShouldEqual(1);
    [Fact] void should_keep_the_scope_label() => _request.ConcurrencyScopes.Single().EventSourceId.ShouldEqual(_sourceId.Value);
    [Fact] void should_keep_the_explicit_position() => _request.ConcurrencyScopes.Single().Scope.SequenceNumber.ShouldEqual(77UL);
    [Fact] void should_keep_the_source_narrowing() => _request.ConcurrencyScopes.Single().Scope.EventSourceId.ShouldBeTrue();
    [Fact] void should_not_inherit_the_definition_source_type() => _request.ConcurrencyScopes.Single().Scope.EventSourceType.ShouldBeNull();
    [Fact] void should_not_inherit_the_definition_stream_type() => _request.ConcurrencyScopes.Single().Scope.EventStreamType.ShouldBeNull();
    [Fact] void should_not_inherit_the_definition_stream_id() => _request.ConcurrencyScopes.Single().Scope.EventStreamId.ShouldBeNull();
    [Fact] void should_still_resolve_event_routing() => _request.Events.Select(@event => @event.EventSourceType).ShouldContainOnly("ShoppingCart", "ShoppingCart");
    [Fact] void should_still_keep_each_event_stream_id() => _request.Events.Select(@event => @event.EventStreamId).ShouldEqual(["first-stream", "second-stream"]);
}
