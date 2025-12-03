// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_getting_target_type_for_enum_type : given.a_json_schema_generator
{
    public enum MyEnum
    {
        First = 0,
        Second = 1,
    }

    public record TypeWithEnum(MyEnum Value);

    IJsonSchemaDocument schema;
    IJsonSchemaProperty property;
    Type type;

    void Establish()
    {
        schema = _generator.Generate(typeof(TypeWithEnum));
        property = schema.ActualProperties.Values.First();
    }

    void Because() => type = property.GetTargetTypeForJsonSchemaProperty(_typeFormats);

    [Fact] void should_be_integer() => type.ShouldEqual(typeof(int));
}
