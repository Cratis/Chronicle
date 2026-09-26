// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_EventValueProviderExpressionResolvers;

public class when_resolving_an_empty_expression : Specification
{
    EventValueProviderExpressionResolvers _resolvers;
    Exception _result;

    void Establish() => _resolvers = new(new TypeFormats(), NullLogger<EventValueProviderExpressionResolvers>.Instance);

    void Because() => _result = Catch.Exception(() => _resolvers.Resolve(new JsonSchemaProperty { Type = JsonObjectType.String }, ""));

    [Fact] void should_reject_the_expression_instead_of_treating_it_as_an_event_property() => _result.ShouldBeOfExactType<UnsupportedEventValueExpression>();
}
