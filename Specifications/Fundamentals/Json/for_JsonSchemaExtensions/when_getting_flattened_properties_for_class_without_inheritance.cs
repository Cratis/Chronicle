// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NJsonSchema;
using NJsonSchema.Generation;

namespace Aksio.Cratis.Json.for_JsonSchemaExtensions;

public class when_getting_flattened_properties_for_class_without_inheritance : Specification
{
    record SimpleType(int SomeInteger, string SomeString);

    JsonSchema schema;

    IEnumerable<JsonSchemaProperty> result;

    void Establish()
    {
        var settings = new JsonSchemaGeneratorSettings();
        var generator = new JsonSchemaGenerator(settings);
        schema = generator.Generate(typeof(SimpleType));
    }

    void Because() => result = schema.GetFlattenedProperties();

    [Fact] void should_get_the_properties_on_the_type() => result.Select(_ => _.Name).ShouldContainOnly(nameof(SimpleType.SomeInteger), nameof(SimpleType.SomeString));
}
