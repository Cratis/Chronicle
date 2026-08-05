// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_ConstraintDefinitionSerializer.when_deserializing_a_unique_event_type_constraint;

/// <summary>
/// The definition carried a single event type before the constraint could cover several. Reading such a
/// document into the current record without mapping it leaves the covered event types absent, and every
/// reader dereferences them - registration compares each stored definition with the incoming one, so one
/// legacy definition takes down constraint registration for the whole event store and the client never
/// finishes connecting. Upgrading it to a one-element sequence keeps the constraint's meaning.
/// </summary>
public class and_it_was_persisted_with_a_single_event_type : given.a_stored_constraint_definition
{
    static readonly ConstraintName _name = ConstraintNameValue;
    static readonly EventTypeId _eventTypeId = "the-event-type";

    IConstraintDefinition _result;

    void Because() => _result = Read(LegacyUniqueEventTypeDocument(_eventTypeId.Value));

    [Fact] void should_deserialize_as_a_unique_event_type_constraint() => _result.ShouldBeOfExactType<UniqueEventTypeConstraintDefinition>();
    [Fact] void should_keep_the_constraint_name() => _result.Name.ShouldEqual(_name);
    [Fact] void should_cover_the_single_event_type_it_was_persisted_with() => ((UniqueEventTypeConstraintDefinition)_result).EventTypeIds.ShouldContainOnly([_eventTypeId]);
    [Fact] void should_be_comparable_with_an_incoming_definition() => _result.Equals(new UniqueEventTypeConstraintDefinition(_name, [_eventTypeId])).ShouldBeTrue();
}
