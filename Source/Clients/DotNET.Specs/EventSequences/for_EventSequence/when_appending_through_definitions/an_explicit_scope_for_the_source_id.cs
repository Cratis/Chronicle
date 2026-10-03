// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_through_definitions;

public class an_explicit_scope_for_the_source_id : given.a_definition_backed_batch
{
    ConcurrencyScope _explicit;

    void Establish() => _explicit = new ConcurrencyScope(77UL, _sourceId);

    async Task Because() => await Append([Item("2025-01"), Item("2025-02")], new Dictionary<EventSourceId, ConcurrencyScope> { [_sourceId] = _explicit });

    [Fact] void should_not_fail() => _exception.ShouldBeNull();
    [Fact] void should_send_the_explicit_scope() => _request.ConcurrencyScopes.Single().Scope.SequenceNumber.ShouldEqual(77UL);
    [Fact] void should_not_broaden_it() => _request.ConcurrencyScopes.Single().Scope.EventStreamId.ShouldBeNull();
}
