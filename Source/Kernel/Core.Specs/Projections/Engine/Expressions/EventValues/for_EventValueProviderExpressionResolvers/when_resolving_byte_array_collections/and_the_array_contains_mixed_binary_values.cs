// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_EventValueProviderExpressionResolvers.when_resolving_byte_array_collections;

public class and_the_array_contains_mixed_binary_values : given.an_appended_event
{
    EventValueProviderExpressionResolvers _resolvers;
    object _result;

    void Establish()
    {
        _resolvers = new(new TypeFormats(), Substitute.For<ILogger<EventValueProviderExpressionResolvers>>());
        ((IDictionary<string, object?>)@event.Content)["Payload"] = new object?[] { Array.Empty<byte>(), new byte[] { 1 }, "+/8=", null };
    }

    void Because() => _result = _resolvers.Resolve(new JsonSchemaProperty
    {
        Type = JsonObjectType.Array,
        Item = new JsonSchema { Type = JsonObjectType.String, Format = "byte-array" }
    },
    "Payload")(@event);

    [Fact] void should_preserve_the_element_count() => ((List<object>)_result).Count.ShouldEqual(4);
    [Fact] void should_preserve_the_empty_binary_value() => ((byte[])((List<object>)_result)[0]).ShouldBeEmpty();
    [Fact] void should_preserve_the_single_byte_value() => ((byte[])((List<object>)_result)[1]).SequenceEqual<byte>([1]).ShouldBeTrue();
    [Fact] void should_convert_the_base64_value_using_the_item_format() => ((byte[])((List<object>)_result)[2]).SequenceEqual<byte>([251, 255]).ShouldBeTrue();
    [Fact] void should_preserve_the_null_value() => ((List<object>)_result)[3].ShouldBeNull();
}
