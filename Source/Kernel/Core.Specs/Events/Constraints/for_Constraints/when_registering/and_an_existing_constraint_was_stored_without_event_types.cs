// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.when_registering;

/// <summary>
/// The reported symptom, at the level it was reported: a unique event type constraint persisted before the
/// definition covered several event types comes back with none, and registration compares every stored
/// definition against the incoming one. One such definition took down registration for the whole event store,
/// so the client never finished connecting and the only way out was discarding the store.
/// </summary>
public class and_an_existing_constraint_was_stored_without_event_types : given.a_constraints_system
{
    static readonly ConstraintName _name = "SomeRule";
    static readonly EventTypeId _eventTypeId = "the-event-type";

    Exception _exception;

    void Establish() => _stateStorage.State.Constraints.Add(new UniqueEventTypeConstraintDefinition(_name, null!));

    async Task Because() => _exception = await Catch.Exception(() => _constraints.Register([new UniqueEventTypeConstraintDefinition(_name, [_eventTypeId])]));

    [Fact] void should_register_without_failing() => _exception.ShouldBeNull();
    [Fact] void should_keep_a_single_constraint() => _stateStorage.State.Constraints.Count.ShouldEqual(1);
    [Fact] void should_take_the_incoming_event_types() =>
        _stateStorage.State.Constraints.OfType<UniqueEventTypeConstraintDefinition>().Single().EventTypeIds.ShouldContainOnly([_eventTypeId]);
}
