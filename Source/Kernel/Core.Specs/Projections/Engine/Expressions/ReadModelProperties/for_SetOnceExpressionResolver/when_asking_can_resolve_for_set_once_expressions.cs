// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.Expressions.ReadModelProperties.for_SetOnceExpressionResolver;

public class when_asking_can_resolve_for_set_once_expressions : Specification
{
    SetOnceExpressionResolver _resolver;

    void Establish() => _resolver = new(new EventValueProviderExpressionResolvers(new TypeFormats(), NullLogger<EventValueProviderExpressionResolvers>.Instance));

    [Fact] void should_resolve_an_event_property() => _resolver.CanResolve(string.Empty, "$setOnce(name)").ShouldBeTrue();
    [Fact] void should_resolve_a_nested_event_context_expression() => _resolver.CanResolve(string.Empty, "$setOnce($eventContext(occurred))").ShouldBeTrue();
    [Fact] void should_resolve_a_nested_caused_by_expression() => _resolver.CanResolve(string.Empty, "$setOnce($causedBy(subject))").ShouldBeTrue();
    [Fact] void should_resolve_the_event_source_id() => _resolver.CanResolve(string.Empty, "$setOnce($eventSourceId)").ShouldBeTrue();
    [Fact] void should_not_resolve_a_plain_expression() => _resolver.CanResolve(string.Empty, "name").ShouldBeFalse();
    [Fact] void should_not_resolve_an_empty_set_once() => _resolver.CanResolve(string.Empty, "$setOnce()").ShouldBeFalse();
    [Fact] void should_not_resolve_an_unclosed_set_once() => _resolver.CanResolve(string.Empty, "$setOnce(name").ShouldBeFalse();
}
