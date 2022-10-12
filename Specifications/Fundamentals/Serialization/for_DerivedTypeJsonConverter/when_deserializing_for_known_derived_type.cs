// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Aksio.Cratis.Serialization.for_DerivedTypeJsonConverterFactory;

public class when_deserializing_for_known_derived_type : Specification
{
    const string derived_type_id = "230fbbd7-6e87-43c8-a3e5-af69b8fd759d";

    const string json = $"{{\"{DerivedTypeJsonConverter<object>.DerivedTypeIdProperty}\":\"{derived_type_id}\", \"SomeValue\": 42 }}";

    interface ITargetType { }

    [DerivedType(derived_type_id)]
    record DerivedType(int SomeValue) : ITargetType;

    Mock<IDerivedTypes> derived_types;
    DerivedTypeJsonConverter<ITargetType> converter;
    ITargetType result;

    void Establish()
    {
        derived_types = new();
        derived_types.Setup(_ => _.GetDerivedTypeFor(typeof(ITargetType), derived_type_id)).Returns(typeof(DerivedType));
        converter = new(derived_types.Object);
    }

    void Because()
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json).AsSpan());
        result = converter.Read(ref reader, typeof(ITargetType), new JsonSerializerOptions());
    }

    [Fact] void should_return_derived_type_instance() => result.ShouldBeOfExactType<DerivedType>();
    [Fact] void should_deserialize_content() => ((DerivedType)result).SomeValue.ShouldEqual(42);
}
