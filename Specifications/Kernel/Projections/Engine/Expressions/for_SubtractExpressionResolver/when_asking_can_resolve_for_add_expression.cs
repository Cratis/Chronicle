// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.Expressions.for_SubtractExpressionResolver
{
    public class when_asking_can_resolve_for_add_expression : Specification
    {
        SubtractExpressionResolver resolver;
        bool result;

        void Establish() => resolver = new();

        void Because() => result = resolver.CanResolve(string.Empty, "$subtract(something)");

        [Fact] void should_be_able_to_resolve() => result.ShouldBeTrue();
    }
}
