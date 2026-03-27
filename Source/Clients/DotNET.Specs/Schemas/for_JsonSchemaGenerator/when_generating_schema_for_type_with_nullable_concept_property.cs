// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_generating_schema_for_type_with_nullable_concept_property : given.a_json_schema_generator
{
    record TypeWithNullableConcept(EventSequenceNumber? NullableSequenceNumber, EventSequenceNumber NonNullableSequenceNumber);

    JsonSchema _result;

    void Because() => _result = _generator.Generate(typeof(TypeWithNullableConcept));

    [Fact] void should_inject_nullable_uint64_format_for_nullable_concept_property() => _result.ActualProperties["NullableSequenceNumber"].Format.ShouldEqual("uint64?");
    [Fact] void should_inject_uint64_format_for_non_nullable_concept_property() => _result.ActualProperties["NonNullableSequenceNumber"].Format.ShouldEqual("uint64");
}
