// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_ConstraintDefinitionSerializer.when_deserializing_a_unique_event_type_constraint;

/// <summary>
/// An earlier revision of this serializer named the concrete type with a <c>constraintType</c> element of its own
/// instead of the discriminator, and stripped the discriminator on the way out. No shipped kernel ever wrote that
/// shape - the serializer was never on the write path - but a build that registered it would have, and a store in
/// that shape must stay readable rather than fail on a missing discriminator.
/// </summary>
public class and_the_concrete_type_is_named_by_a_constraint_type_element : given.a_stored_constraint_definition
{
    static readonly EventTypeId _eventTypeId = "the-event-type";

    IConstraintDefinition _result;

    void Because() => _result = Read(new BsonDocument
    {
        { "_id", ConstraintNameValue },
        { "eventTypeIds", new BsonArray { _eventTypeId.Value } },
        { "constraintType", nameof(ConstraintType.UniqueEventType) }
    });

    [Fact] void should_deserialize_as_a_unique_event_type_constraint() => _result.ShouldBeOfExactType<UniqueEventTypeConstraintDefinition>();
    [Fact] void should_cover_the_event_type_it_was_persisted_with() => ((UniqueEventTypeConstraintDefinition)_result).EventTypeIds.ShouldContainOnly([_eventTypeId]);
}
