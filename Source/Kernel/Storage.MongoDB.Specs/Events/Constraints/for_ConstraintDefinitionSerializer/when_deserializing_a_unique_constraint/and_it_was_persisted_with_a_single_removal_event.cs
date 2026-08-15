// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_ConstraintDefinitionSerializer.when_deserializing_a_unique_constraint;

/// <summary>
/// The value form is persisted by the same serializer and carried the same single removal event, so it needs the same
/// upgrade. Without it every unique constraint that declares a release becomes unreadable the moment the kernel is
/// upgraded, and registration compares stored definitions against incoming ones before a single constraint is
/// registered.
/// </summary>
public class and_it_was_persisted_with_a_single_removal_event : given.a_stored_constraint_definition
{
    static readonly ConstraintName _name = ConstraintNameValue;
    static readonly EventTypeId _coveredEventTypeId = "InvitationSent";
    static readonly EventTypeId _removalEventTypeId = "InvitationRevoked";

    IConstraintDefinition _result;

    void Because() => _result = Read(new BsonDocument
    {
        { "_t", nameof(UniqueConstraintDefinition) },
        { "_id", ConstraintNameValue },
        {
            "eventDefinitions",
            new BsonArray
            {
                new BsonDocument
                {
                    { "eventTypeId", _coveredEventTypeId.Value },
                    { "properties", new BsonArray { "EmailAddress" } }
                }
            }
        },
        { "removedWith", _removalEventTypeId.Value },
        { "ignoreCasing", false }
    });

    [Fact] void should_deserialize_as_a_unique_constraint() => _result.ShouldBeOfExactType<UniqueConstraintDefinition>();
    [Fact] void should_keep_the_constraint_name() => _result.Name.ShouldEqual(_name);
    [Fact] void should_keep_releasing_on_the_event_it_was_persisted_with() => ((UniqueConstraintDefinition)_result).RemovedWith.ShouldContainOnly([_removalEventTypeId]);
}
