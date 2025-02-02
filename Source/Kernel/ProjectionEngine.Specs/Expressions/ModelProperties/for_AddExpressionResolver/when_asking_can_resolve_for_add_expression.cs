// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ProjectionEngine.Expressions.EventValues;

namespace Cratis.Chronicle.ProjectionEngine.Expressions.ModelProperties.for_AddExpressionResolver;

public class when_asking_can_resolve_for_add_expression : Specification
{
    IEventValueProviderExpressionResolvers _eventValueResolvers;
    AddExpressionResolver _resolver;
    bool _result;

    void Establish()
    {
        _eventValueResolvers = Substitute.For<IEventValueProviderExpressionResolvers>();
        _resolver = new(_eventValueResolvers);
    }

    void Because() => _result = _resolver.CanResolve(string.Empty, "$add(something)");

    [Fact] void should_be_able_to_resolve() => _result.ShouldBeTrue();
}
