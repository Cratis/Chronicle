// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Serialization.for_DerivedTypes;

public class when_getting_target_type_for_known_derived_type : given.derived_types
{
    interface ITargetType { }

    [DerivedType("fc13ac34-b69b-4438-8ebc-bc91bb5e2ee6")]
    record DerivedType : ITargetType { }

    DerivedTypes derived_types;
    Type result;

    protected override IEnumerable<Type> ProvideDerivedTypes() => new[] { typeof(DerivedType) };

    void Establish() => derived_types = new DerivedTypes(types.Object);

    void Because() => result = derived_types.GetTargetTypeFor(typeof(DerivedType));

    [Fact] void should_return_correct_target_type() => result.ShouldEqual(typeof(ITargetType));
}
