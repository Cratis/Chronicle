// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Serialization.for_DerivedTypes;

public class when_initializing_with_type_with_target_type_but_no_target_types_implemented : given.derived_types
{
    interface IFirst { }
    interface ISecond { }

    [DerivedType("fc13ac34-b69b-4438-8ebc-bc91bb5e2ee6", typeof(ISecond))]
    record DerivedType { }

    Exception result;

    protected override IEnumerable<Type> ProvideDerivedTypes() => new[] { typeof(DerivedType) };

    void Because() => result = Catch.Exception(() => _ = new DerivedTypes(types.Object));

    [Fact] void should_throw_missing_target_type() => result.ShouldBeOfExactType<MissingTargetTypeForDerivedType>();
}
