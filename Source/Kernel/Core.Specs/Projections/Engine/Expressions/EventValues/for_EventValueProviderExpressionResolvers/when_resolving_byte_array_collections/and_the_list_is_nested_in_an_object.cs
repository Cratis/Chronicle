// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_EventValueProviderExpressionResolvers.when_resolving_byte_array_collections;

public class and_the_list_is_nested_in_an_object : given.an_appended_event
{
    EventValueProviderExpressionResolvers _resolvers;
    JsonSchemaProperty _schema;
    object _result;

    void Establish()
    {
        _resolvers = new(new TypeFormats(), Substitute.For<ILogger<EventValueProviderExpressionResolvers>>());
        var payload = new ExpandoObject();
        ((IDictionary<string, object?>)payload)["Chunks"] = new List<byte[]> { Array.Empty<byte>(), new byte[] { 1 }, new byte[] { 251, 255 } };
        ((IDictionary<string, object?>)@event.Content)["Payload"] = payload;
        _schema = new JsonSchemaProperty { Type = JsonObjectType.Object };
        _schema.Properties["Chunks"] = new JsonSchemaProperty
        {
            Type = JsonObjectType.Array,
            Item = new JsonSchema { Type = JsonObjectType.String, Format = "byte-array" }
        };
    }

    void Because() => _result = _resolvers.Resolve(_schema, "Payload")(@event);

    [Fact] void should_preserve_the_element_count() => ((IList)((IDictionary<string, object>)_result)["Chunks"]).Count.ShouldEqual(3);
    [Fact] void should_preserve_the_empty_chunk() => ((byte[])((IList)((IDictionary<string, object>)_result)["Chunks"])[0]).ShouldBeEmpty();
    [Fact] void should_preserve_the_single_byte_chunk() => ((byte[])((IList)((IDictionary<string, object>)_result)["Chunks"])[1]).SequenceEqual<byte>([1]).ShouldBeTrue();
    [Fact] void should_preserve_the_multi_byte_chunk() => ((byte[])((IList)((IDictionary<string, object>)_result)["Chunks"])[2]).SequenceEqual<byte>([251, 255]).ShouldBeTrue();
}
