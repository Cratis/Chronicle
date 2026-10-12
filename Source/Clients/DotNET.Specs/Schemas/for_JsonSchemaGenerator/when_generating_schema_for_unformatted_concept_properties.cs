// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_generating_schema_for_unformatted_concept_properties : given.a_json_schema_generator
{
    JsonSchema _result;

    void Establish() => _generator.Generate(typeof(given.TypeWithUnformattedConcepts));

    void Because() => _result = _generator.GenerateForReadModel(typeof(given.TypeWithUnformattedConcepts));

    [Fact] void should_preserve_nullable_string_concept_nullability() => _result.ActualProperties["NullableString"].IsNullable().ShouldBeTrue();
    [Fact] void should_preserve_nullable_bool_concept_nullability() => _result.ActualProperties["NullableBool"].IsNullable().ShouldBeTrue();
    [Fact] void should_preserve_the_nullable_string_representation() => _result.ActualProperties["NullableString"].Type.ShouldEqual(JsonObjectType.String | JsonObjectType.Null);
    [Fact] void should_preserve_the_nullable_bool_representation() => _result.ActualProperties["NullableBool"].Type.ShouldEqual(JsonObjectType.Boolean | JsonObjectType.Null);
    [Fact] void should_keep_non_nullable_string_concepts_non_nullable() => _result.ActualProperties["NonNullableString"].IsNullable().ShouldBeFalse();
    [Fact] void should_keep_non_nullable_bool_concepts_non_nullable() => _result.ActualProperties["NonNullableBool"].IsNullable().ShouldBeFalse();
    [Fact] void should_keep_the_non_nullable_string_representation() => _result.ActualProperties["NonNullableString"].Type.ShouldEqual(JsonObjectType.String);
    [Fact] void should_keep_the_non_nullable_bool_representation() => _result.ActualProperties["NonNullableBool"].Type.ShouldEqual(JsonObjectType.Boolean);
}
