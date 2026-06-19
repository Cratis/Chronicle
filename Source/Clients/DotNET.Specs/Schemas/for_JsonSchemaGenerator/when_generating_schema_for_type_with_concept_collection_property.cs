// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_generating_schema_for_type_with_concept_collection_property : given.a_json_schema_generator
{
    record StringConcept(string Value) : ConceptAs<string>(Value);
    record GuidConcept(Guid Value) : ConceptAs<Guid>(Value);
    record TypeWithConceptCollections(
        IReadOnlyList<StringConcept> StringConcepts,
        IEnumerable<GuidConcept> GuidConcepts,
        StringConcept ScalarConcept);

    JsonSchema _result;

    void Because() => _result = _generator.Generate(typeof(TypeWithConceptCollections));

    [Fact] void should_make_string_concept_list_an_array() => _result.ActualProperties["StringConcepts"].Type.ShouldEqual(JsonObjectType.Array);
    [Fact] void should_give_string_concept_list_string_items() => _result.ActualProperties["StringConcepts"].Item!.Type.ShouldEqual(JsonObjectType.String);
    [Fact] void should_make_guid_concept_list_an_array() => _result.ActualProperties["GuidConcepts"].Type.ShouldEqual(JsonObjectType.Array);
    [Fact] void should_give_guid_concept_list_string_items() => _result.ActualProperties["GuidConcepts"].Item!.Type.ShouldEqual(JsonObjectType.String);
    [Fact] void should_give_guid_concept_list_items_the_underlying_primitive_format() => _result.ActualProperties["GuidConcepts"].Item!.Format.ShouldEqual("guid");
    [Fact] void should_keep_scalar_concept_as_underlying_primitive() => _result.ActualProperties["ScalarConcept"].Type.ShouldEqual(JsonObjectType.String);
}
