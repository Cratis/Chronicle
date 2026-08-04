// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_ConstraintDefinitionSerializer.when_serializing_a_constraint_definition;

/// <summary>
/// The serializer exists to upgrade older documents on read, and putting it on the read path also puts it on the
/// write path. It must therefore write byte for byte what the driver's own discriminated-interface path writes, or
/// a store would silently change shape underneath a kernel that does not have it - which is every kernel that
/// wrote the store in the first place.
/// </summary>
public class and_it_is_a_unique_event_type_constraint : given.a_stored_constraint_definition
{
    static readonly UniqueEventTypeConstraintDefinition _definition = new(ConstraintNameValue, ["the-first-event-type", "the-second-event-type"]);

    BsonDocument _written;

    void Because() => _written = Write(_definition);

    [Fact] void should_write_what_the_driver_writes_for_the_concrete_type() => _written.ShouldEqual(_definition.ToBsonDocument(typeof(UniqueEventTypeConstraintDefinition)));
    [Fact] void should_name_the_concrete_type_with_the_discriminator() => _written["_t"].AsString.ShouldEqual(nameof(UniqueEventTypeConstraintDefinition));
    [Fact] void should_not_introduce_an_element_of_its_own() => _written.Contains("constraintType").ShouldBeFalse();
    [Fact] void should_read_back_as_the_definition_that_was_written() => Read(_written).ShouldEqual(_definition);
    [Fact] void should_round_trip_without_changing_the_document() => Write(Read(_written)).ShouldEqual(_written);
}
