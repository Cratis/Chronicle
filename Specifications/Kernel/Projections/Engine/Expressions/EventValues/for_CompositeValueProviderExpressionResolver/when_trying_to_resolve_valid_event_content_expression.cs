// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Strings;

namespace Aksio.Cratis.Events.Projections.Expressions.EventValues.for_CompositeValueProviderExpressionResolver;

public class when_trying_to_resolve_valid_event_content_expression : given.an_appended_event
{
    CompositeValueProviderExpressionResolver resolver;
    object result;

    void Establish() => resolver = new();

    void Because() => result = resolver.Resolve($"$composite({nameof(MyEvent.Something).ToCamelCase()}, {nameof(MyEvent.SomethingElse).ToCamelCase()})")(@event);

    [Fact] void should_resolve_to_a_value_provider_that_gets_value_from_event_content() => result.ShouldEqual($"{MyEvent.Something}+{MyEvent.SomethingElse}");
}
