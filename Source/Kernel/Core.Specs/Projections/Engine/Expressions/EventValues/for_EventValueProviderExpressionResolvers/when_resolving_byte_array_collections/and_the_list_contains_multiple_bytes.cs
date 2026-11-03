// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_EventValueProviderExpressionResolvers.when_resolving_byte_array_collections;

public class and_the_list_contains_multiple_bytes : given.an_appended_event
{
    EventValueProviderExpressionResolvers _resolvers;
    object _result;

    void Establish()
    {
        _resolvers = new(new TypeFormats(), Substitute.For<ILogger<EventValueProviderExpressionResolvers>>());
        ((IDictionary<string, object?>)@event.Content)["Payload"] = new List<byte[]> { new byte[] { 251, 255 }, new byte[] { 0, 128, 255 } };
    }

    void Because() => _result = _resolvers.Resolve(new JsonSchemaProperty
    {
        Type = JsonObjectType.Array,
        Item = new JsonSchema { Type = JsonObjectType.String, Format = "byte-array" }
    },
    "Payload")(@event);

    [Fact] void should_preserve_the_element_count() => ((List<object>)_result).Count.ShouldEqual(2);
    [Fact] void should_preserve_the_first_chunk() => ((byte[])((List<object>)_result)[0]).SequenceEqual<byte>([251, 255]).ShouldBeTrue();
    [Fact] void should_preserve_the_second_chunk() => ((byte[])((List<object>)_result)[1]).SequenceEqual<byte>([0, 128, 255]).ShouldBeTrue();
}
