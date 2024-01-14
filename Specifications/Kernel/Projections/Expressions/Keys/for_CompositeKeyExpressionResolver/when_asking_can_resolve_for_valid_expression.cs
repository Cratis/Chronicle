// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Projections.Expressions.Keys.for_CompositeKeyExpressionResolver;

public class when_asking_can_resolve_for_valid_expression : given.a_resolver
{
    bool result;

    void Because() => result = resolver.CanResolve("$composite(prop1=value,prop2=value2)");

    [Fact] void should_be_able_to_resolve() => result.ShouldBeTrue();
}
