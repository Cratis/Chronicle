// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.Expressions.ReadModelProperties.for_AddExpressionResolver;

public class when_asking_can_resolve_for_malformed_operand : Specification
{
    bool _result;

    void Because() => _result = new AddExpressionResolver(
        Substitute.For<IEventValueProviderExpressionResolvers>(),
        Substitute.For<ITypeFormats>()).CanResolve(string.Empty, $"{WellKnownExpressions.Add}(1e--3)");

    [Fact] void should_reject_the_operand() => _result.ShouldBeFalse();
}
