// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Expressions.for_ReadModelPropertyExpressionResolvers;

public class when_trying_to_resolve_unknown_expression : given.read_model_property_expression_resolvers
{
    Exception _result;

    void Because() => _result = Catch.Exception(() => _resolvers.Resolve(string.Empty, new(), "$randomUnknownExpression"));

    [Fact] void should_throw_unsupported_event_value_expression() => _result.ShouldBeOfExactType<UnsupportedReadModelPropertyExpression>();
}
