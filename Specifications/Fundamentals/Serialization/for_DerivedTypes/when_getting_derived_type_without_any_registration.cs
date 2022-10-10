// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Serialization.for_DerivedTypes;

public class when_getting_derived_type_without_any_registration : given.derived_types
{
    DerivedTypes derived_types;
    Exception result;

    void Establish() => derived_types = new DerivedTypes(types.Object);

    void Because() => result = Catch.Exception(() => _ = derived_types.GetDerivedTypeFor(typeof(object), "7ece19d8-2312-4335-a49e-3da5e88e2941"));

    [Fact] void should_throw_missing_derived_type_for_target_type() => result.ShouldBeOfExactType<MissingDerivedTypeForTargetType>();
}
