// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_EventValueProviderExpressionResolvers;

public class when_resolving_a_formatted_scalar : given.an_appended_event
{
    EventValueProviderExpressionResolvers _resolvers;
    object _result;

    void Establish()
    {
        _resolvers = new(new TypeFormats(), Substitute.For<ILogger<EventValueProviderExpressionResolvers>>());
        ((IDictionary<string, object?>)@event.Content)["Payload"] = "2147483648";
    }

    void Because() => _result = _resolvers.Resolve(new JsonSchemaProperty { Type = JsonObjectType.Integer, Format = "int64" }, "Payload")(@event);

    [Fact] void should_convert_to_the_formatted_type() => _result.ShouldBeOfExactType<long>();
    [Fact] void should_convert_the_value() => _result.ShouldEqual(2147483648L);
}
