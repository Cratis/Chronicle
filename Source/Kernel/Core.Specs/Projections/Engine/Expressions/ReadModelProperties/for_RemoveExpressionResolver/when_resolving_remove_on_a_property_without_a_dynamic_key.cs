// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.Expressions.ReadModelProperties.for_RemoveExpressionResolver;

public class when_resolving_remove_on_a_property_without_a_dynamic_key : Specification
{
    RemoveExpressionResolver _resolver;
    Exception _error;

    void Establish() => _resolver = new();

    void Because() => _error = Catch.Exception(() => _resolver.Resolve("slots", new JsonSchemaProperty(), "$remove"));

    [Fact] void should_be_able_to_resolve_the_expression() => _resolver.CanResolve("slots", "$remove").ShouldBeTrue();
    [Fact] void should_fail_because_removing_needs_a_dynamic_key() => _error.ShouldBeOfExactType<RemoveRequiresDynamicPropertyPath>();
}
