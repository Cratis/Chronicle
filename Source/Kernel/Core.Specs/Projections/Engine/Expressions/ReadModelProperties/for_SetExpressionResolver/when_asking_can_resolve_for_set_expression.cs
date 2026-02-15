// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Expressions.EventValues;

namespace Cratis.Chronicle.Projections.Engine.Expressions.ReadModelProperties.for_SetExpressionResolver;

public class when_asking_can_resolve_for_set_expression : Specification
{
    IEventValueProviderExpressionResolvers _eventValueResolvers;
    SetExpressionResolver _resolver;
    bool _result;

    void Establish()
    {
        _eventValueResolvers = Substitute.For<IEventValueProviderExpressionResolvers>();
        _eventValueResolvers.CanResolve("something").Returns(true);
        _resolver = new(_eventValueResolvers);
    }

    void Because() => _result = _resolver.CanResolve(string.Empty, "something");

    [Fact] void should_ask_event_value_resolvers() => _eventValueResolvers.Received(1).CanResolve("something");
    [Fact] void should_be_able_to_resolve() => _result.ShouldBeTrue();
}
