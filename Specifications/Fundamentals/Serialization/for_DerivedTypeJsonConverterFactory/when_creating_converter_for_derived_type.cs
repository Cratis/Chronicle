// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Aksio.Cratis.Serialization.for_DerivedTypeJsonConverterFactory;

public class when_creating_converter_for_derived_type : given.a_derived_type_json_converter_factory
{
    interface ITargetType { }
    class DerivedType : ITargetType { }

    JsonConverter converter;

    void Establish() => derived_types.Setup(_ => _.GetTargetTypeFor(typeof(DerivedType))).Returns(typeof(ITargetType));

    void Because() => converter = factory.CreateConverter(typeof(DerivedType), null!);

    [Fact] void should_return_new_converter() => converter.ShouldNotBeNull();
    [Fact] void should_be_a_derived_attribute_converter_for_the_target_type() => converter.GetType().ShouldEqual(typeof(DerivedTypeJsonConverter<ITargetType>));
}
