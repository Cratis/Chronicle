// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.Expressions.EventValues.for_CompositeFromEventContextValueProviderExpressionResolver;

public class when_trying_to_resolve_valid_composite_event_context_expression : given.an_appended_event
{
    CompositeFromEventContextValueProviderExpressionResolver resolver;
    object result;

    void Establish() => resolver = new();

    void Because() => result = resolver.Resolve("$compositeFromContext(occurred.year, occurred.month, occurred.day)")(@event);

    [Fact] void should_resolve_to_a_value_provider_that_gets_value_from_event_content() => result.ShouldEqual($"{@event.Context.Occurred.Year}+{@event.Context.Occurred.Month}+{@event.Context.Occurred.Day}");
}
