// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_ConstraintDefinitionSerializer.when_deserializing_a_unique_constraint;

/// <summary>
/// Every definition persisted before a constraint could declare its event sequences has no element for them. It must
/// read back as applying to every event sequence - the behavior it was registered with - without a migration.
/// </summary>
public class and_it_was_persisted_before_it_could_declare_event_sequences : given.a_stored_constraint_definition
{
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
                    { "eventTypeId", "InvitationSent" },
                    { "properties", new BsonArray { "EmailAddress" } }
                }
            }
        },
        { "removedWith", new BsonArray() },
        { "ignoreCasing", false }
    });

    [Fact] void should_declare_no_event_sequences() => ((UniqueConstraintDefinition)_result).EventSequences.ShouldBeEmpty();
    [Fact] void should_apply_to_every_event_sequence() => _result.AppliesTo(EventSequenceId.Outbox).ShouldBeTrue();
}
