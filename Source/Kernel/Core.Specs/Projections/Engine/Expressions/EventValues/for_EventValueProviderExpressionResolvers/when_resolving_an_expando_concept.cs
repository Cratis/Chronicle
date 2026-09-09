// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_EventValueProviderExpressionResolvers;

public class when_resolving_an_expando_concept : given.an_appended_event
{
    EventValueProviderExpressionResolvers _resolvers;
    object _result;

    void Establish()
    {
        _resolvers = new(new TypeFormats(), Substitute.For<ILogger<EventValueProviderExpressionResolvers>>());
        var concept = new ExpandoObject();
        ((IDictionary<string, object?>)concept)["value"] = "42";
        ((IDictionary<string, object?>)@event.Content)["Payload"] = concept;
    }

    void Because() => _result = _resolvers.Resolve(new JsonSchemaProperty { Type = JsonObjectType.Integer }, "Payload")(@event);

    [Fact] void should_unwrap_and_convert_the_concept() => _result.ShouldEqual(42);
    [Fact] void should_convert_to_the_target_type() => _result.ShouldBeOfExactType<int>();
}
