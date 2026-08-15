// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_EventValueProviderExpressionResolvers;

/// <summary>
/// The clear resolver has to be registered, not merely written - an unregistered one leaves $null falling through
/// to the unsupported-expression throw.
/// </summary>
public class when_asking_can_resolve_for_the_null_expression : Specification
{
    EventValueProviderExpressionResolvers _resolvers;
    bool _result;

    void Establish() => _resolvers = new(new TypeFormats(), Substitute.For<ILogger<EventValueProviderExpressionResolvers>>());

    void Because() => _result = _resolvers.CanResolve(WellKnownExpressions.Null);

    [Fact] void should_be_able_to_resolve() => _result.ShouldBeTrue();
}
