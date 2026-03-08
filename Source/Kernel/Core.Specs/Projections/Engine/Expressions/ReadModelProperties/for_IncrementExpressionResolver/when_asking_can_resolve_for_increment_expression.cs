// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.Expressions.ReadModelProperties.for_IncrementExpressionResolver;

public class when_asking_can_resolve_for_increment_expression : Specification
{
    IncrementExpressionResolver _resolver;
    bool _result;

    void Establish() => _resolver = new(new TypeFormats());

    void Because() => _result = _resolver.CanResolve(string.Empty, WellKnownExpressions.Increment);

    [Fact] void should_be_able_to_resolve() => _result.ShouldBeTrue();
}
