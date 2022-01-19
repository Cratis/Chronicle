// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.Expressions.for_PropertyMapperExpressionResolvers
{
    public class when_trying_to_resolve_unknown_expression : Specification
    {
        PropertyMapperExpressionResolvers resolvers;
        Exception result;

        void Establish() => resolvers = new PropertyMapperExpressionResolvers();

        void Because() => result = Catch.Exception(() => resolvers.Resolve(string.Empty, "$randomUnknownExpression"));

        [Fact] void should_throw_unsupported_event_value_expression() => result.ShouldBeOfExactType<UnsupportedPropertyMapperExpression>();
    }
}
