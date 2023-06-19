// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Strings;
using NJsonSchema;

namespace Aksio.Cratis.Schemas.for_JsonSchemaExtensions;

public class when_getting_flattened_properties_for_class_with_inheritance : given.a_json_schema_generator
{
    record BaseType(int BaseInteger, string BaseString);
    record SimpleType(int SomeInteger, string SomeString) : BaseType(42, string.Empty);

    JsonSchema schema;

    IEnumerable<JsonSchemaProperty> result;

    void Establish() => schema = generator.Generate(typeof(SimpleType));

    void Because() => result = schema.GetFlattenedProperties();

    [Fact]
    void should_get_all_properties() => result.Select(_ => _.Name).ShouldContainOnly(
            nameof(SimpleType.SomeInteger).ToCamelCase(),
            nameof(SimpleType.SomeString).ToCamelCase(),
            nameof(BaseType.BaseInteger).ToCamelCase(),
            nameof(BaseType.BaseString).ToCamelCase());
}
