// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_EventValueProviderExpressionResolvers;

/// <summary>
/// Resolution runs the value through type conversion against the target schema property. A clear has to survive
/// that untouched - converting no value to a string type would produce an empty string, which is the very sentinel
/// the clear exists to avoid writing.
/// </summary>
public class when_resolving_the_null_expression : given.an_appended_event
{
    EventValueProviderExpressionResolvers _resolvers;
    object _result;

    void Establish() => _resolvers = new(new TypeFormats(), Substitute.For<ILogger<EventValueProviderExpressionResolvers>>());

    void Because() => _result = _resolvers.Resolve(new JsonSchemaProperty { Type = JsonObjectType.String }, WellKnownExpressions.Null)(@event);

    [Fact] void should_provide_no_value() => _result.ShouldBeNull();
}
