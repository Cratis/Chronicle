// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Expressions.Keys.for_CompositeKeyExpressionResolver;

public class when_resolving_a_malformed_typed_expression : given.a_resolver
{
    Exception _result;

    void Because() => _result = Catch.Exception(() => _resolver.Resolve(_projection, "$composite(OrderKey, first=one, broken)", string.Empty));

    [Fact] void should_reject_the_invalid_mapping() => _result.ShouldBeOfExactType<InvalidCompositeKeyPropertyMappingExpression>();
}
