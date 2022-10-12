// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Aksio.Cratis.Serialization.for_DerivedTypeJsonConverterFactory;

public class when_creating_converter_for_derived_type : given.a_derived_type_json_converter_factory
{
    interface ITargetType { }

    JsonConverter converter;

    void Because() => converter = factory.CreateConverter(typeof(ITargetType), null!);

    [Fact] void should_return_new_converter() => converter.ShouldNotBeNull();
    [Fact] void should_be_a_derived_attribute_converter_for_the_target_type() => converter.GetType().ShouldEqual(typeof(DerivedTypeJsonConverter<ITargetType>));
}
