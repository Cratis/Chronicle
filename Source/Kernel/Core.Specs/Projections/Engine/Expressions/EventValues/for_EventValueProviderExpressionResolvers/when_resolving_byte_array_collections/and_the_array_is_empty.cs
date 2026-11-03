// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_EventValueProviderExpressionResolvers.when_resolving_byte_array_collections;

public class and_the_array_is_empty : given.an_appended_event
{
    EventValueProviderExpressionResolvers _resolvers;
    object _result;

    void Establish()
    {
        _resolvers = new(new TypeFormats(), Substitute.For<ILogger<EventValueProviderExpressionResolvers>>());
        ((IDictionary<string, object?>)@event.Content)["Payload"] = Array.Empty<byte[]>();
    }

    void Because() => _result = _resolvers.Resolve(new JsonSchemaProperty
    {
        Type = JsonObjectType.Array,
        Item = new JsonSchema { Type = JsonObjectType.String, Format = "byte-array" }
    },
    "Payload")(@event);

    [Fact] void should_preserve_the_empty_collection() => ((List<object>)_result).ShouldBeEmpty();
}
