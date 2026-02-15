// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.Expressions.ReadModelProperties.for_SubtractExpressionResolver;

public class when_asking_can_resolve_for_add_expression : Specification
{
    IEventValueProviderExpressionResolvers _eventValueResolvers;
    ITypeFormats _typeFormats;
    SubtractExpressionResolver _resolver;
    bool _result;

    void Establish()
    {
        _eventValueResolvers = Substitute.For<IEventValueProviderExpressionResolvers>();
        _typeFormats = Substitute.For<ITypeFormats>();
        _resolver = new(_eventValueResolvers, _typeFormats);
    }

    void Because() => _result = _resolver.CanResolve(string.Empty, $"{WellKnownExpressions.Subtract}(something)");

    [Fact] void should_be_able_to_resolve() => _result.ShouldBeTrue();
}
