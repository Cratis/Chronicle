// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_ConstraintDefinitionSerializer.when_deserializing_a_unique_event_type_constraint;

/// <summary>
/// The removal event was a single optional value and is persisted under the same element name, so a document written
/// by an earlier kernel holds a string where an array is now expected. The driver refuses that outright, and
/// registration compares every stored definition with the incoming one — so one such document would take down
/// constraint registration for the whole event store and the client would never finish connecting.
/// </summary>
public class and_it_was_persisted_with_a_single_removal_event : given.a_stored_constraint_definition
{
    static readonly ConstraintName _name = ConstraintNameValue;
    static readonly EventTypeId _coveredEventTypeId = "the-event-type";
    static readonly EventTypeId _removalEventTypeId = "the-removal-event-type";

    IConstraintDefinition _result;

    void Because() => _result = Read(LegacySingleRemovalDocument([_coveredEventTypeId.Value], _removalEventTypeId.Value));

    [Fact] void should_deserialize_as_a_unique_event_type_constraint() => _result.ShouldBeOfExactType<UniqueEventTypeConstraintDefinition>();
    [Fact] void should_keep_the_constraint_name() => _result.Name.ShouldEqual(_name);
    [Fact] void should_keep_releasing_on_the_event_it_was_persisted_with() => ((UniqueEventTypeConstraintDefinition)_result).RemovedWith.ShouldContainOnly([_removalEventTypeId]);
    [Fact] void should_be_comparable_with_an_incoming_definition() => _result.Equals(new UniqueEventTypeConstraintDefinition(_name, [_coveredEventTypeId], [_removalEventTypeId])).ShouldBeTrue();
}
