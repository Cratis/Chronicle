// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_LiteralExpressionResolver;

public class when_asking_can_resolve_for_non_literals : Specification
{
    LiteralExpressionResolver _resolver;
    bool[] _results;

    void Establish() => _resolver = new();

    void Because() => _results = [
        _resolver.CanResolve(""),
        _resolver.CanResolve("status"),
        _resolver.CanResolve("version2"),
        _resolver.CanResolve("status.code"),
        _resolver.CanResolve("\"unfinished"),
        _resolver.CanResolve("$eventSourceId"),
        _resolver.CanResolve("1,5")];

    [Fact] void should_not_claim_event_paths_or_unsupported_expressions() => _results.All(_ => !_).ShouldBeTrue();
}
