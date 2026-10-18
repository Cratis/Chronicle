// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_through_definitions;

public class with_a_unit_enrolling_two_guards_in_one_batch : given.a_definition_backed_batch
{
    UnitOfWork _unit;

    void Establish()
    {
        ((IKernelCapabilities)_connection).Capabilities.Returns([KernelCapabilities.ConcurrencyScopeConjunctions]);
        var store = Substitute.For<IEventStore>();
        store.GetEventSequence(EventSequenceId.Log).Returns(_eventSequence);
        _unit = new(CorrelationId.New(), _ => { }, store);
        _unit.AddEvents(
            EventSequenceId.Log,
            [new(_sourceId, "approval")],
            [
                new(_sourceId, new ConcurrencyScope(7, _sourceId, "Approvals", "sent")),
                new(_sourceId, new ConcurrencyScope(9, _sourceId, "Corrections", "open"))
            ]);
    }

    Task Because() => _unit.Commit();

    [Fact] void should_not_overwrite_enrollment_for_the_same_source() => _request.ConcurrencyScopes.Count().ShouldEqual(2);
    [Fact] void should_preserve_each_boundary() => _request.ConcurrencyScopes.Select(_ => _.Scope.SequenceNumber).ShouldContainOnly(7UL, 9UL);
    [Fact] void should_append_the_event_once() => _request.Events.Count().ShouldEqual(1);
}
