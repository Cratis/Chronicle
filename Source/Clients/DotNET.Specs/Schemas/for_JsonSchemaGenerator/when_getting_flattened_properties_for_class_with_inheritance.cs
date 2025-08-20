// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NJsonSchema;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_getting_flattened_properties_for_class_with_inheritance : given.a_json_schema_generator
{
    record BaseType(int BaseInteger, string BaseString);
    record SimpleType(int SomeInteger, string SomeString) : BaseType(42, string.Empty);

    JsonSchema _schema;

    IEnumerable<JsonSchemaProperty> _result;

    void Establish() => _schema = _generator.Generate(typeof(SimpleType));

    void Because() => _result = _schema.GetFlattenedProperties();

    [Fact]
    void should_get_all_properties() => _result.Select(_ => _.Name).ShouldContainOnly(
            nameof(SimpleType.SomeInteger),
            nameof(SimpleType.SomeString),
            nameof(BaseType.BaseInteger),
            nameof(BaseType.BaseString));
}
