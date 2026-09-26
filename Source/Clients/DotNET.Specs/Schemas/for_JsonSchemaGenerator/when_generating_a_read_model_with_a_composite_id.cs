// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_generating_a_read_model_with_a_composite_id : given.a_json_schema_generator
{
    record OrderKey(string CustomerId, string OrderNumber);
    record OrderReadModel(OrderKey Id);

    JsonSchema _schema;
    JsonSchema _previousSchema;

    void Because()
    {
        _schema = _generator.Generate(typeof(OrderReadModel));
        var previousNode = JsonNode.Parse(_schema.ToJson())!.AsObject();
        previousNode["properties"]!["Id"]!.AsObject().Remove("title");
        _previousSchema = new JsonSchema(previousNode);
    }

    [Fact] void should_name_the_inline_key_type() => _schema.Properties["Id"].ActualSchema.Title.ShouldEqual(nameof(OrderKey));
    [Fact] void should_keep_the_key_properties() => _schema.Properties["Id"].ActualSchema.Properties.Keys.ShouldContain("CustomerId");
    [Fact] void should_not_move_the_key_to_a_definition() => _schema.Definitions.ShouldBeEmpty();
    [Fact] void should_keep_an_existing_schema_compatible() => _previousSchema.IsCompatibleWith(_schema).ShouldBeTrue();
}
