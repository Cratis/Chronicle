// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Expressions.Keys.for_CompositeKeyExpressionResolver;

public class when_asking_can_resolve_for_invalid_expression : given.a_resolver
{
    bool result;

    void Because() => result = _resolver.CanResolve("$unsupported");

    [Fact] void should_not_be_able_to_resolve() => result.ShouldBeFalse();
}
