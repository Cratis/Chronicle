// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_ConstraintDefinitionSerializer.when_deserializing_a_unique_event_type_constraint;

/// <summary>
/// Most constraints declare no removal event at all, and an earlier kernel persisted that as a null element rather
/// than by omitting it. Reading the null through to the definition would leave the removal events absent where every
/// reader expects a sequence, which is the failure the covered event types already had to be rescued from.
/// </summary>
public class and_it_was_persisted_without_a_removal_event : given.a_stored_constraint_definition
{
    static readonly ConstraintName _name = ConstraintNameValue;
    static readonly EventTypeId _coveredEventTypeId = "the-event-type";

    IConstraintDefinition _result;

    void Because() => _result = Read(LegacySingleRemovalDocument([_coveredEventTypeId.Value], null));

    [Fact] void should_deserialize_as_a_unique_event_type_constraint() => _result.ShouldBeOfExactType<UniqueEventTypeConstraintDefinition>();
    [Fact] void should_release_on_nothing() => ((UniqueEventTypeConstraintDefinition)_result).RemovedWith.ShouldBeEmpty();
    [Fact] void should_be_comparable_with_an_incoming_definition() => _result.Equals(new UniqueEventTypeConstraintDefinition(_name, [_coveredEventTypeId])).ShouldBeTrue();
}
