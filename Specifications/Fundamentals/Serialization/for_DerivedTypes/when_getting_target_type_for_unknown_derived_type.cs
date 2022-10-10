// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Serialization.for_DerivedTypes;

public class when_getting_target_type_for_unknown_derived_type : given.derived_types
{
    DerivedTypes derived_types;
    Exception result;

    void Establish() => derived_types = new DerivedTypes(types.Object);

    void Because() => result = Catch.Exception(() => _ = derived_types.GetTargetTypeFor(typeof(object)));

    [Fact] void should_throw_missing_target_type_for_derived_type() => result.ShouldBeOfExactType<MissingTargetTypeForDerivedType>();
}
