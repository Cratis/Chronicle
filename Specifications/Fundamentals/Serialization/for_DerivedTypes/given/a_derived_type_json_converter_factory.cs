// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Serialization.for_DerivedTypeJsonConverterFactory.given;

public class a_derived_type_json_converter_factory : Specification
{
    protected DerivedTypeJsonConverterFactory factory;
    protected Mock<IDerivedTypes> derived_types;

    void Establish()
    {
        derived_types = new();
        factory = new(derived_types.Object);
    }
}
