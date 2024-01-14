// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Projections.Expressions.EventValues.for_ValueExpressionResolver;

public class when_trying_to_resolve_valid_value_expression : given.an_appended_event
{
    ValueExpressionResolver resolver;
    object result;

    void Establish() => resolver = new();

    void Because() => result = resolver.Resolve("$value(42)")(@event);

    [Fact] void should_resolve_to_a_value_provider_that_returns_the_value() => result.ShouldEqual("42");
}
