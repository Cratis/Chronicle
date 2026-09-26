// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_EventContentExpressionResolver;

public class when_asking_can_resolve_for_nested_and_indexed_paths : Specification
{
    EventContentExpressionResolver _resolver;
    bool[] _results;

    void Establish() => _resolver = new();

    void Because() => _results = [
        _resolver.CanResolve("version2"),
        _resolver.CanResolve("address.city"),
        _resolver.CanResolve("items[0].price")];

    [Fact] void should_still_claim_property_paths() => _results.All(_ => _).ShouldBeTrue();
}
