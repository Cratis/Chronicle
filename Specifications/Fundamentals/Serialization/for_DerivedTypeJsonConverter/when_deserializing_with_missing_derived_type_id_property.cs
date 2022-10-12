// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Aksio.Cratis.Serialization.for_DerivedTypeJsonConverterFactory;

public class when_deserializing_with_missing_derived_type_id_property : Specification
{
    const string derived_type_id = "230fbbd7-6e87-43c8-a3e5-af69b8fd759d";
    interface ITargetType { }
    Mock<IDerivedTypes> derived_types;
    DerivedTypeJsonConverter<ITargetType> converter;
    ITargetType result;

    void Establish()
    {
        derived_types = new();
        converter = new(derived_types.Object);
    }

    void Because()
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes("{}").AsSpan());
        result = converter.Read(ref reader, typeof(ITargetType), new JsonSerializerOptions());
    }

    [Fact] void should_return_null_value() => result.ShouldBeNull();
}
