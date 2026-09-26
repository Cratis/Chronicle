// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_EventContentExpressionResolver;

public class when_asking_can_resolve_for_literals_and_empty_expressions : Specification
{
    EventContentExpressionResolver _resolver;
    bool[] _results;

    void Establish() => _resolver = new();

    void Because() => _results = [
        _resolver.CanResolve("\"draft\""),
        _resolver.CanResolve("\"unfinished"),
        _resolver.CanResolve("True"),
        _resolver.CanResolve("false"),
        _resolver.CanResolve("1"),
        _resolver.CanResolve("-2.5"),
        _resolver.CanResolve("")];

    [Fact] void should_not_claim_values_that_are_not_property_paths() => _results.Any(_ => _).ShouldBeFalse();
}
