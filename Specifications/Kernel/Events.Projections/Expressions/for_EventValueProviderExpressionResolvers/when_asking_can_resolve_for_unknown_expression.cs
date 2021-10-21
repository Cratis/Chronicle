// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections.Expressions.for_EventValueProviderExpressionResolvers
{
    public class when_asking_can_resolve_for_unknown_expression : Specification
    {
        EventValueProviderExpressionResolvers resolvers;
        bool result;

        void Establish() => resolvers = new EventValueProviderExpressionResolvers();

        void Because() => result = resolvers.CanResolve(string.Empty, "$randomUnknownExpression");

        [Fact] void should_not_be_able_to_resolve() => result.ShouldBeFalse();
    }
}
