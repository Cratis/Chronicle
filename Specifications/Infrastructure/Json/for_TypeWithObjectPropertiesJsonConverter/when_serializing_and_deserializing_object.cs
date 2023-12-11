// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Aksio.Cratis.Json.for_TypeWithObjectPropertiesJsonConverter;

public class when_serializing_and_deserializing_object : Specification
{
    SuperType super_type;
    SuperType result;
    JsonSerializerOptions options;

    void Establish()
    {
        options = new JsonSerializerOptions();
        options.Converters.Add(new TypeWithObjectPropertiesJsonConverterFactory<Converter, TypeWithObjectProperties>());

        super_type = new SuperType("Hello", 42, new OtherType(43, Guid.NewGuid()), new FirstType("World"), "Something");
    }

    void Because()
    {
        var serialized = JsonSerializer.Serialize(super_type, options);
        result = JsonSerializer.Deserialize<SuperType>(serialized, options);
    }

    [Fact] void should_be_equal() => result.ShouldEqual(super_type);
}
