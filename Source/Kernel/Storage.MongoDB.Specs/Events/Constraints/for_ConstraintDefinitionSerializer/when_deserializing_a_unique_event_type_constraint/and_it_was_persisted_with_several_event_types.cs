// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_ConstraintDefinitionSerializer.when_deserializing_a_unique_event_type_constraint;

/// <summary>
/// The current shape has to keep reading as it always did - the upgrade of the older one must not touch it. This is
/// the shape every document in a 16.12 or later store is in, so it is also what guards against putting the
/// serializer on the read path making that store unreadable.
/// </summary>
public class and_it_was_persisted_with_several_event_types : given.a_stored_constraint_definition
{
    static readonly ConstraintName _name = ConstraintNameValue;
    static readonly EventTypeId _firstEventTypeId = "the-first-event-type";
    static readonly EventTypeId _secondEventTypeId = "the-second-event-type";

    IConstraintDefinition _result;

    void Because() => _result = Read(new BsonDocument
    {
        { "_t", nameof(UniqueEventTypeConstraintDefinition) },
        { "_id", _name.Value },
        { "scope", BsonNull.Value },
        { "eventTypeIds", new BsonArray { _firstEventTypeId.Value, _secondEventTypeId.Value } }
    });

    [Fact] void should_deserialize_as_a_unique_event_type_constraint() => _result.ShouldBeOfExactType<UniqueEventTypeConstraintDefinition>();
    [Fact] void should_cover_every_event_type_it_was_persisted_with() => ((UniqueEventTypeConstraintDefinition)_result).EventTypeIds.ShouldContainOnly([_firstEventTypeId, _secondEventTypeId]);
}
