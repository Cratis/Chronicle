// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Aksio.Cratis.Serialization.for_DerivedTypeJsonConverterFactory;

public class when_asking_can_convert_on_type_without_attribute : given.a_derived_type_json_converter_factory
{
    bool result;

    void Because() => result = factory.CanConvert(typeof(object));

    [Fact] void should_not_be_able_to_convert() => result.ShouldBeFalse();
}
