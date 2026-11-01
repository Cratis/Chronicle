// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.TypeSets.for_ObserverTypeSet.when_getting_for_an_observer;

public class and_it_has_stored_type_set_state : given.a_type_set_store
{
    readonly Guid _registrationId = Guid.NewGuid();
    readonly Guid _operationId = Guid.NewGuid();
    readonly DateTimeOffset _started = new(2026, 10, 1, 12, 0, 0, TimeSpan.FromHours(2));
    ObserverTypeSet _result;

    void Establish()
    {
        var control = new ObserverTypeSetControl(
            new(7),
            new("subscriber", ObserverFilters.None),
            new Dictionary<EventTypeId, Storage.Observation.ObserverTypeSetMember>
            {
                ["event-a"] = new(new(3), new(123)),
                ["event-b"] = new(new(5), null),
                ["event-z"] = new(new(1), new(0))
            },
            [new("connection", _registrationId, [new("event-a", 2, true)], EventGenerationDelivery.Compatibility), new("legacy-connection", null, [], EventGenerationDelivery.Compatibility)])
        {
            PendingChange = new(new(8), _started, new(456), ["event-c"], [new(_operationId, "event-b")]),
            RetirementResults = [new(_operationId, "event-d", Storage.Observation.ObserverEventTypeRetirementOutcome.Retired, new(2), ["declaring-connection"])]
        };
        var state = ObserverState.Empty with
        {
            TypeSetControl = control,
            Initialization = new(Storage.Observation.ObserverInitializationState.Initializing, ["unsettled handover"])
        };
        _observers.GetReadOnly(Arg.Any<ObserverId>()).Returns(state);
    }

    async Task Because() => _result = await ObserverTypeSet.GetForObserver("store", "namespace", "observer", _storage);

    [Fact] void should_map_revision() => _result.Revision.ShouldEqual(7L);
    [Fact] void should_map_member_incarnation() => _result.Members.Single(_ => _.EventTypeId == "event-a").Incarnation.ShouldEqual(3L);
    [Fact] void should_not_use_the_migrating_get() => _observers.DidNotReceive().Get(Arg.Any<ObserverId>());
    [Fact] void should_map_join_fence() => _result.Members.Single(_ => _.EventTypeId == "event-a").JoinFence.ShouldEqual(new ObserverTypeSetFence(123UL));
    [Fact] void should_keep_a_zero_fence_distinct_from_no_fence() => _result.Members.Single(_ => _.EventTypeId == "event-z").JoinFence.ShouldEqual(new ObserverTypeSetFence(0UL));
    [Fact] void should_preserve_an_unfenced_member() => _result.Members.Single(_ => _.EventTypeId == "event-b").JoinFence.ShouldBeNull();
    [Fact] void should_map_connection() => _result.Registrations.First().ConnectionId.ShouldEqual("connection");
    [Fact] void should_map_registration_identity() => _result.Registrations.First().RegistrationId.ShouldEqual(_registrationId);
    [Fact] void should_preserve_legacy_registration_identity() => _result.Registrations.Last().RegistrationId.ShouldBeNull();
    [Fact] void should_map_declared_types() => _result.Registrations.First().DeclaredTypes.Single().ShouldEqual(new ObserverDeclaredEventType("event-a", 2, true));
    [Fact] void should_map_pending_revision() => _result.PendingChange.Revision.ShouldEqual(8L);
    [Fact] void should_map_pending_start() => _result.PendingChange.Started.ShouldEqual(_started);
    [Fact] void should_map_initial_tail() => _result.PendingChange.InitialTail.ShouldEqual(456UL);
    [Fact] void should_map_additions() => _result.PendingChange.Additions.Single().ShouldEqual("event-c");
    [Fact] void should_map_pending_retirements() => _result.PendingChange.Retirements.Single().ShouldEqual(new ObserverEventTypeRetirementRequest(_operationId, "event-b"));
    [Fact] void should_map_retirement_identity() => _result.RetirementResults.Single().OperationId.ShouldEqual(_operationId);
    [Fact] void should_map_retirement_event_type() => _result.RetirementResults.Single().EventTypeId.ShouldEqual("event-d");
    [Fact] void should_map_retirement_outcome() => _result.RetirementResults.Single().Outcome.ShouldEqual(ObserverEventTypeRetirementOutcome.Retired);
    [Fact] void should_map_retirement_incarnation() => _result.RetirementResults.Single().Incarnation.ShouldEqual(new ObserverTypeSetIncarnation(2L));
    [Fact] void should_map_declaring_connections() => _result.RetirementResults.Single().DeclaringConnections.Single().ShouldEqual("declaring-connection");
    [Fact] void should_map_initialization_state() => _result.InitializationState.ShouldEqual(ObserverInitializationState.Initializing);
    [Fact] void should_map_refusal_reasons() => _result.RefusalReasons.Single().ShouldEqual("unsettled handover");
    [Fact] void should_not_write_state() => _observers.ReceivedCalls().Count().ShouldEqual(1);
}
