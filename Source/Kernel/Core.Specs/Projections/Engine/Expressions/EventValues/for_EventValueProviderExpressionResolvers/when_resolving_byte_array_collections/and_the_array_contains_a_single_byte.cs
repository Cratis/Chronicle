// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_EventValueProviderExpressionResolvers.when_resolving_byte_array_collections;

public class and_the_array_contains_a_single_byte : given.an_appended_event
{
    EventValueProviderExpressionResolvers _resolvers;
    object _result;

    void Establish()
    {
        _resolvers = new(new TypeFormats(), Substitute.For<ILogger<EventValueProviderExpressionResolvers>>());
        ((IDictionary<string, object?>)@event.Content)["Payload"] = new byte[][] { [1] };
    }

    void Because() => _result = _resolvers.Resolve(new JsonSchemaProperty
    {
        Type = JsonObjectType.Array,
        Item = new JsonSchema { Type = JsonObjectType.String, Format = "byte-array" }
    },
    "Payload")(@event);

    [Fact] void should_preserve_the_element_count() => ((List<object>)_result).Count.ShouldEqual(1);
    [Fact] void should_preserve_the_binary_type() => ((List<object>)_result)[0].ShouldBeOfExactType<byte[]>();
    [Fact] void should_preserve_the_byte() => ((byte[])((List<object>)_result)[0]).SequenceEqual<byte>([1]).ShouldBeTrue();
}
