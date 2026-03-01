// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Projections.Engine.Expressions.Keys.for_CompositeKeyExpressionResolver;

public class when_resolving_expression_missing_content : given.a_resolver
{
    Exception _result;

    void Because() => _result = Catch.Exception(() => _resolver.Resolve(_projection, $"{WellKnownExpressions.Composite}()", string.Empty));

    [Fact] void should_throw_missing_composite_expression() => _result.ShouldBeOfExactType<MissingCompositeExpressions>();
}
