// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_ConstraintDefinitionSerializer.when_deserializing_a_unique_event_type_constraint;

/// <summary>
/// The current shape has to keep reading as it always did - the upgrade of the older one must not touch it.
/// </summary>
public class and_it_was_persisted_with_several_event_types : given.a_constraint_definition_serializer
{
    readonly ConstraintName _name = "some-constraint";
    readonly EventTypeId _firstEventTypeId = "the-first-event-type";
    readonly EventTypeId _secondEventTypeId = "the-second-event-type";

    IConstraintDefinition _result;

    void Because() => _result = Deserialize(new BsonDocument
    {
        { "_id", _name.Value },
        { "eventTypeIds", new BsonArray { _firstEventTypeId.Value, _secondEventTypeId.Value } },
        { "constraintType", nameof(ConstraintType.UniqueEventType) }
    });

    [Fact] void should_cover_every_event_type_it_was_persisted_with() => ((UniqueEventTypeConstraintDefinition)_result).EventTypeIds.ShouldContainOnly([_firstEventTypeId, _secondEventTypeId]);
}
