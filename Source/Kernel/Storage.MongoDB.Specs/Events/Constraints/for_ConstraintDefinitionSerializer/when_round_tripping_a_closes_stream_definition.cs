// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_ConstraintDefinitionSerializer;

public class when_round_tripping_a_closes_stream_definition : given.a_stored_constraint_definition
{
    ClosesStreamConstraintDefinition _definition;
    BsonDocument _written;
    IConstraintDefinition _read;

    void Establish() => _definition = new(ConstraintNameValue, ["Closed", "Cancelled"], ClosedStreamDimensions.EventSourceId | ClosedStreamDimensions.EventStreamId, ["Reopened"], "period")
    {
        EventSequences = [EventSequenceId.Log]
    };

    void Because()
    {
        _written = Write(_definition);
        _read = Read(_written);
    }

    [Fact] void should_name_the_concrete_type() => _written["_t"].AsString.ShouldEqual(nameof(ClosesStreamConstraintDefinition));
    [Fact] void should_keep_every_declaration_field() => _read.ShouldEqual(_definition);
    [Fact] void should_round_trip_without_changing_the_document() => Write(_read).ShouldEqual(_written);
}
