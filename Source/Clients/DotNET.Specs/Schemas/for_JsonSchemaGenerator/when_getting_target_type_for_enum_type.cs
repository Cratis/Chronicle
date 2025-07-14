// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NJsonSchema;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_getting_target_type_for_enum_type : given.a_json_schema_generator
{
    public enum MyEnum
    {
        First = 0,
        Second = 1,
    }

    public record TypeWithEnum(MyEnum Value);

    JsonSchema _schema;
    JsonSchemaProperty _property;
    Type _type;

    void Establish()
    {
        _schema = generator.Generate(typeof(TypeWithEnum));
        _property = _schema.ActualProperties.Values.First();
    }

    void Because() => _type = _property.GetTargetTypeForJsonSchemaProperty(type_formats);

    [Fact] void should_be_integer() => _type.ShouldEqual(typeof(int));
}
