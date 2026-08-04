// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_ConstraintDefinitionSerializer.when_serializing_a_constraint_definition;

/// <summary>
/// The other implementation of the interface goes through the same serializer, so it has the same obligation not to
/// change shape - and has no upgrade to apply on the way back in.
/// </summary>
public class and_it_is_a_unique_constraint : given.a_stored_constraint_definition
{
    static readonly UniqueConstraintDefinition _definition = new(ConstraintNameValue, [new UniqueConstraintEventDefinition("the-event-type", ["the-property"])]);

    BsonDocument _written;

    void Because() => _written = Write(_definition);

    [Fact] void should_write_what_the_driver_writes_for_the_concrete_type() => _written.ShouldEqual(_definition.ToBsonDocument(typeof(UniqueConstraintDefinition)));
    [Fact] void should_name_the_concrete_type_with_the_discriminator() => _written["_t"].AsString.ShouldEqual(nameof(UniqueConstraintDefinition));
    [Fact] void should_read_back_as_the_same_concrete_type() => Read(_written).ShouldBeOfExactType<UniqueConstraintDefinition>();
    [Fact] void should_round_trip_without_changing_the_document() => Write(Read(_written)).ShouldEqual(_written);
}
