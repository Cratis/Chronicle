// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NJsonSchema;
using NJsonSchema.Generation;

namespace Aksio.Cratis.Json.for_JsonSchemaExtensions;

public class when_getting_flattened_properties_for_class_with_inheritance : Specification
{
    record BaseType(int BaseInteger, string BaseString);
    record SimpleType(int SomeInteger, string SomeString) : BaseType(42, string.Empty);

    JsonSchema schema;

    IEnumerable<JsonSchemaProperty> result;

    void Establish()
    {
        var settings = new JsonSchemaGeneratorSettings();
        var generator = new JsonSchemaGenerator(settings);
        schema = generator.Generate(typeof(SimpleType));
    }

    void Because() => result = schema.GetFlattenedProperties();

    [Fact]
    void should_get_all_properties() => result.Select(_ => _.Name).ShouldContainOnly(
            nameof(SimpleType.SomeInteger),
            nameof(SimpleType.SomeString),
            nameof(BaseType.BaseInteger),
            nameof(BaseType.BaseString));
}
