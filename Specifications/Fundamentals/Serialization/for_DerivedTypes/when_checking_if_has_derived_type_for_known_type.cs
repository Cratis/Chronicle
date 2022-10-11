// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Serialization.for_DerivedTypes;

public class when_checking_if_has_derived_type_for_known_type : given.derived_types
{
    interface ITargetType { }

    [DerivedType("fc13ac34-b69b-4438-8ebc-bc91bb5e2ee6")]
    class DerivedType : ITargetType { }

    bool result;

    protected override IEnumerable<Type> ProvideDerivedTypes() => new[] { typeof(DerivedType) };

    DerivedTypes derived_types;

    void Establish() => derived_types = new(types.Object);

    void Because() => result = derived_types.IsDerivedType(typeof(DerivedType));

    [Fact] void should_consider_it_a_derived_type() => result.ShouldBeTrue();
}
