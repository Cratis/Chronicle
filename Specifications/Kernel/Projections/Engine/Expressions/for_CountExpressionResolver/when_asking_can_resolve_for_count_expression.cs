// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.Expressions.for_CountExpressionResolver;

public class when_asking_can_resolve_for_count_expression : Specification
{
    CountExpressionResolver resolver;
    bool result;

    void Establish() => resolver = new();

    void Because() => result = resolver.CanResolve(string.Empty, "$count()");

    [Fact] void should_be_able_to_resolve() => result.ShouldBeTrue();
}
