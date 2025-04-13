// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Expressions.ModelProperties.for_DecrementExpressionResolver;

public class when_asking_can_resolve_for_decrement_expression : Specification
{
    DecrementExpressionResolver resolver;
    bool result;

    void Establish() => resolver = new(new TypeFormats());

    void Because() => result = resolver.CanResolve(string.Empty, "$decrement()");

    [Fact] void should_be_able_to_resolve() => result.ShouldBeTrue();
}
