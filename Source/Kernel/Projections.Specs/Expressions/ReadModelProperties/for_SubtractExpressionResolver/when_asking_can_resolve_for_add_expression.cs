// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Expressions.EventValues;

namespace Cratis.Chronicle.Projections.Expressions.ReadModelProperties.for_SubtractExpressionResolver;

public class when_asking_can_resolve_for_add_expression : Specification
{
    IEventValueProviderExpressionResolvers _eventValueResolvers;
    SubtractExpressionResolver _resolver;
    bool _result;

    void Establish()
    {
        _eventValueResolvers = Substitute.For<IEventValueProviderExpressionResolvers>();
        _resolver = new(_eventValueResolvers);
    }

    void Because() => _result = _resolver.CanResolve(string.Empty, "$subtract(something)");

    [Fact] void should_be_able_to_resolve() => _result.ShouldBeTrue();
}
