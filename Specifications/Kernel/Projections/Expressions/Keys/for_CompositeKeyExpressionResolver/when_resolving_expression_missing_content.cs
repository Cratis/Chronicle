// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Expressions.Keys.for_CompositeKeyExpressionResolver;

public class when_resolving_expression_missing_content : given.a_resolver
{
    Exception result;

    void Because() => result = Catch.Exception(() => resolver.Resolve(projection.Object, "$composite()", string.Empty));

    [Fact] void should_throw_missing_composite_expression() => result.ShouldBeOfExactType<MissingCompositeExpressions>();
}
