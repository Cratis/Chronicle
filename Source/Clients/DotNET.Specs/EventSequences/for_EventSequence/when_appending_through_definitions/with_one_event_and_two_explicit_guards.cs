// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_through_definitions;

public class with_one_event_and_two_explicit_guards : given.a_definition_backed_batch
{
    void Establish() => ((IKernelCapabilities)_connection).Capabilities.Returns([KernelCapabilities.ConcurrencyScopeConjunctions]);

    async Task Because()
    {
        KeyValuePair<EventSourceId, ConcurrencyScope>[] scopes =
        [
            new(_sourceId, new(7, _sourceId, "Approvals", "sent")),
            new(_sourceId, new(EventSequenceNumber.BeforeFirst, _sourceId, "Corrections", "open"))
        ];
        await _eventSequence.AppendMany([new(_sourceId, "approval")], null, null, scopes);
    }

    [Fact] void should_use_the_multi_source_rpc_for_one_event() => _request.Events.Count().ShouldEqual(1);
    [Fact] void should_send_both_guards() => _request.ConcurrencyScopes.Count().ShouldEqual(2);
    [Fact] void should_keep_the_first_expected_tail() => _request.ConcurrencyScopes.First().Scope.SequenceNumber.ShouldEqual(7UL);
    [Fact] void should_keep_the_negative_read_guard() => _request.ConcurrencyScopes.Last().Scope.ExpectsNoMatchingEvent.ShouldBeTrue();
}
