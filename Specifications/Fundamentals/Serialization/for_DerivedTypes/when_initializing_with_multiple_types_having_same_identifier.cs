// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Serialization.for_DerivedTypes;

public class when_initializing_with_multiple_types_having_same_identifier : given.derived_types
{
    interface ITargetType { }

    [DerivedType("fc13ac34-b69b-4438-8ebc-bc91bb5e2ee6")]
    class FirstDerivedType : ITargetType { }

    [DerivedType("fc13ac34-b69b-4438-8ebc-bc91bb5e2ee6")]
    class SecondDerivedType : ITargetType { }

    Exception result;

    protected override IEnumerable<Type> ProvideDerivedTypes() => new[] { typeof(FirstDerivedType), typeof(SecondDerivedType) };

    void Because() => result = Catch.Exception(() => _ = new DerivedTypes(types.Object));

    [Fact] void should_throw_ambiguous_type_identifiers() => result.ShouldBeOfExactType<AmbiguousDerivedTypeIdentifiers>();
}
