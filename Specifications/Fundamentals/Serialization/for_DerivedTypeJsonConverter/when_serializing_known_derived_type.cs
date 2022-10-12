// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Aksio.Cratis.Serialization.for_DerivedTypeJsonConverterFactory;

public class when_serializing_known_derived_type : Specification
{
    const string derived_type_id = "230fbbd7-6e87-43c8-a3e5-af69b8fd759d";
    const string json = $"{{\"someValue\":42,\"{DerivedTypeJsonConverter<object>.DerivedTypeIdProperty}\":\"{derived_type_id}\"}}";
    interface ITargetType { }

    [DerivedType(derived_type_id)]
    record DerivedType(int SomeValue) : ITargetType;

    Mock<IDerivedTypes> derived_types;
    DerivedTypeJsonConverter<ITargetType> converter;
    MemoryStream stream;
    Utf8JsonWriter writer;
    DerivedType input;
    string result;

    void Establish()
    {
        derived_types = new();
        derived_types.Setup(_ => _.GetDerivedTypeFor(typeof(ITargetType), derived_type_id)).Returns(typeof(DerivedType));
        converter = new(derived_types.Object);
        stream = new();
        writer = new(stream);
        input = new(42);
    }

    void Because()
    {
        converter.Write(writer, input, default);
        writer.Flush();
        result = Encoding.UTF8.GetString(stream.ToArray());
    }

    [Fact] void should_convert_to_expected_json() => result.ShouldEqual(json);
}
