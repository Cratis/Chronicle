// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_round_tripping;

public class with_pending_type_set_change : given.an_observer_state_storage
{
    static readonly Guid _operation = Guid.Parse("11111111-1111-1111-1111-111111111111");
    static readonly DateTimeOffset _started = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);
    KernelObserverState _saved;

    async Task Because()
    {
        var control = new ObserverTypeSetControl(new(7), new("reactor-subscriber", ObserverFilters.None), new Dictionary<EventTypeId, ObserverTypeSetMember>(), [])
        {
            RetirementResults =
            [
                new(_operation, new("event-a"), ObserverEventTypeRetirementOutcome.Retired, new(3), []),
                new(Guid.Parse("22222222-2222-2222-2222-222222222222"), new("event-b"), ObserverEventTypeRetirementOutcome.Refused, null, [new("connection-one"), new("connection-two")]),
                new(Guid.Parse("33333333-3333-3333-3333-333333333333"), new("event-c"), ObserverEventTypeRetirementOutcome.NotMember, null, [])
            ],
            PendingChange = new(new(8), _started, new(42), [new("event-b")], [new(_operation, new("event-a"))])
        };
        await _storage.Save(new() { Identifier = "observer", TypeSetControl = control }, Concepts.EventSequences.EventSequenceId.Log);
        _saved = await _storage.Get("observer");
    }

    [Fact] void should_keep_the_retirement_operation() => _saved.TypeSetControl.RetirementResults[0].OperationId.ShouldEqual(_operation);
    [Fact] void should_keep_the_removed_incarnation() => _saved.TypeSetControl.RetirementResults[0].Incarnation.Value.ShouldEqual(3);
    [Fact] void should_keep_the_retirement_outcome() => _saved.TypeSetControl.RetirementResults[0].Outcome.ShouldEqual(ObserverEventTypeRetirementOutcome.Retired);
    [Fact] void should_keep_the_retired_type() => _saved.TypeSetControl.RetirementResults[0].EventTypeId.Value.ShouldEqual("event-a");
    [Fact] void should_keep_the_refused_outcome() => _saved.TypeSetControl.RetirementResults[1].Outcome.ShouldEqual(ObserverEventTypeRetirementOutcome.Refused);
    [Fact] void should_keep_declaring_connections() => _saved.TypeSetControl.RetirementResults[1].DeclaringConnections.Select(connection => connection.Value).ShouldEqual(["connection-one", "connection-two"]);
    [Fact] void should_not_invent_a_refused_incarnation() => _saved.TypeSetControl.RetirementResults[1].Incarnation.ShouldBeNull();
    [Fact] void should_keep_the_not_member_outcome() => _saved.TypeSetControl.RetirementResults[2].Outcome.ShouldEqual(ObserverEventTypeRetirementOutcome.NotMember);
    [Fact] void should_not_invent_a_not_member_incarnation() => _saved.TypeSetControl.RetirementResults[2].Incarnation.ShouldBeNull();
    [Fact] void should_keep_the_pending_revision() => _saved.TypeSetControl.PendingChange.Revision.Value.ShouldEqual(8);
    [Fact] void should_keep_the_change_start() => _saved.TypeSetControl.PendingChange.Started.ShouldEqual(_started);
    [Fact] void should_keep_the_initial_tail() => _saved.TypeSetControl.PendingChange.InitialTail.Value.ShouldEqual(42UL);
    [Fact] void should_keep_pending_additions_by_type_id() => _saved.TypeSetControl.PendingChange.Additions.Select(type => type.Value).ShouldEqual(["event-b"]);
    [Fact] void should_keep_pending_retirements() => _saved.TypeSetControl.PendingChange.Retirements[0].OperationId.ShouldEqual(_operation);
    [Fact] void should_keep_the_pending_retirement_type() => _saved.TypeSetControl.PendingChange.Retirements[0].EventTypeId.Value.ShouldEqual("event-a");
    [Fact] void should_keep_the_committed_revision() => _saved.TypeSetControl.Revision.Value.ShouldEqual(7);
}
