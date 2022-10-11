// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Serialization.for_DerivedTypes;

public class when_checking_if_has_derived_type_for_unknown_type : given.derived_types
{
    bool result;
    DerivedTypes derived_types;

    void Establish() => derived_types = new(types.Object);

    void Because() => result = derived_types.IsDerivedType(typeof(object));

    [Fact] void should_not_consider_it_a_derived_type() => result.ShouldBeFalse();
}
