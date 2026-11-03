// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_EventValueProviderExpressionResolvers;

public class when_resolving_nested_formatted_arrays : given.an_appended_event
{
    EventValueProviderExpressionResolvers _resolvers;
    object _result;

    void Establish()
    {
        _resolvers = new(new TypeFormats(), Substitute.For<ILogger<EventValueProviderExpressionResolvers>>());
        ((IDictionary<string, object?>)@event.Content)["Payload"] = new object[] { new[] { "2147483648", "9223372036854775807" } };
    }

    void Because() => _result = _resolvers.Resolve(new JsonSchemaProperty
    {
        Type = JsonObjectType.Array,
        Item = new JsonSchema
        {
            Type = JsonObjectType.Array,
            Item = new JsonSchema { Type = JsonObjectType.Integer, Format = "int64" }
        }
    },
    "Payload")(@event);

    [Fact] void should_preserve_the_outer_element_count() => ((List<object>)_result).Count.ShouldEqual(1);
    [Fact] void should_convert_the_nested_elements_using_their_format() => ((List<object>)((List<object>)_result)[0]).SequenceEqual([2147483648L, long.MaxValue]).ShouldBeTrue();
}
