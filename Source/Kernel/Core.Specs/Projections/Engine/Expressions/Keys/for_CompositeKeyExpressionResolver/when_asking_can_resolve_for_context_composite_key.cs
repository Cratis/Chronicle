// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Expressions.Keys.for_CompositeKeyExpressionResolver;

public class when_asking_can_resolve_for_context_composite_key : given.a_resolver
{
    bool _result;

    void Because() => _result = _resolver.CanResolve("$composite(Key, namespace=$eventContext.@namespace,number=number)");

    [Fact] void should_accept_context_property_expressions() => _result.ShouldBeTrue();
}
