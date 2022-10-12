// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Serialization.for_DerivedTypes;

public class when_initializing_with_type_with_multiple_target_types : given.derived_types
{
    interface IFirst { }
    interface ISecond { }

    [DerivedType("fc13ac34-b69b-4438-8ebc-bc91bb5e2ee6")]
    record DerivedType : IFirst, ISecond { }

    Exception result;

    protected override IEnumerable<Type> ProvideDerivedTypes() => new[] { typeof(DerivedType) };

    void Because() => result = Catch.Exception(() => _ = new DerivedTypes(types.Object));

    [Fact] void should_throw_ambiguous_target_type() => result.ShouldBeOfExactType<AmbiguousTargetTypeForDerivedType>();
}
