// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Serialization.for_DerivedTypeJsonConverterFactory;

public class when_asking_can_convert_on_type_with_attribute : given.a_derived_type_json_converter_factory
{
    [DerivedType("fc13ac34-b69b-4438-8ebc-bc91bb5e2ee6")]
    record DerivedType { }

    bool result;

    void Because() => result = factory.CanConvert(typeof(DerivedType));

    [Fact] void should_be_able_to_convert() => result.ShouldBeTrue();
}
