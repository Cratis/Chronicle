// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Projections.Expressions.EventValues.for_EventContentExpressionResolver;

public class when_trying_to_resolve_valid_event_content_expression : given.an_appended_event
{
    EventContentExpressionResolver resolver;
    object result;

    void Establish() => resolver = new();

    void Because() => result = resolver.Resolve("something")(@event);

    [Fact] void should_resolve_to_a_value_provider_that_gets_value_from_event_content() => result.ShouldEqual(my_event.Something);
}
