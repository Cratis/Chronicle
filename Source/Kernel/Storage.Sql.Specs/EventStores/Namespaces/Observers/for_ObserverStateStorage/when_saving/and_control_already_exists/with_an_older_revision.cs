// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_saving.and_control_already_exists;

public class with_an_older_revision : given.an_observer_state_storage
{
    static readonly EventTypeId _type = new("event-a");
    ObserverTypeSetControl _control;
    KernelObserverState _saved;

    async Task Establish()
    {
        _control = new(
            new(7),
            new("reactor-subscriber", ObserverFilters.None),
            new Dictionary<EventTypeId, ObserverTypeSetMember> { [_type] = new(new(7), new(42)) },
            [new("connection-new", null, [new(_type, new(2))], EventGenerationDelivery.Pinned)])
        {
            RetirementResults = [new(Guid.NewGuid(), _type, ObserverEventTypeRetirementOutcome.Retired, new(3), [])],
            PendingChange = new(new(8), DateTimeOffset.UtcNow, new(42), [new("event-b")], [])
        };
        await _storage.Save(new() { Identifier = "observer", TypeSetControl = _control }, Concepts.EventSequences.EventSequenceId.Log);
    }

    async Task Because()
    {
        var stale = _control with
        {
            Revision = new(6),
            Members = new Dictionary<EventTypeId, ObserverTypeSetMember> { [_type] = new(new(3), new(12)) },
            Registrations = [],
            RetirementResults = null,
            PendingChange = null
        };
        await _storage.Save(new() { Identifier = "observer", TypeSetControl = stale, NextEventSequenceNumber = new(123) }, Concepts.EventSequences.EventSequenceId.Log);
        _saved = await _storage.Get("observer");
    }

    [Fact] void should_preserve_the_membership_revision() => _saved.TypeSetControl.Revision.Value.ShouldEqual(7);
    [Fact] void should_preserve_the_rejoined_member_incarnation() => _saved.TypeSetControl.Members[_type].Incarnation.Value.ShouldEqual(7);
    [Fact] void should_preserve_the_member_fence() => _saved.TypeSetControl.Members[_type].JoinFence.Value.ShouldEqual(42UL);
    [Fact] void should_preserve_the_connection_capabilities() => _saved.TypeSetControl.Registrations.Single().DeclaredTypes.Single().Generation.Value.ShouldEqual(2U);
    [Fact] void should_preserve_retirement_results() => _saved.TypeSetControl.RetirementResults.Single().OperationId.ShouldEqual(_control.RetirementResults.Single().OperationId);
    [Fact] void should_preserve_the_pending_change() => _saved.TypeSetControl.PendingChange.Revision.Value.ShouldEqual(8);
    [Fact] void should_save_the_other_state_changes() => _saved.NextEventSequenceNumber.Value.ShouldEqual(123UL);
}
