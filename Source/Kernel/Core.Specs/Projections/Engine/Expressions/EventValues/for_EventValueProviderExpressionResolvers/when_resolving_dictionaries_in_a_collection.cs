// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_EventValueProviderExpressionResolvers;

public class when_resolving_dictionaries_in_a_collection : given.an_appended_event
{
    EventValueProviderExpressionResolvers _resolvers;
    Dictionary<object, object> _dictionary;
    List<object?> _input;
    object _result;

    void Establish()
    {
        _resolvers = new(new TypeFormats(), Substitute.For<ILogger<EventValueProviderExpressionResolvers>>());
        _dictionary = new() { ["Name"] = "Upper", ["name"] = "Lower", ["null"] = null! };
        _input = [_dictionary, 42, null];
        ((IDictionary<string, object?>)@event.Content)["Payload"] = _input;
    }

    void Because() => _result = _resolvers.Resolve(new JsonSchemaProperty { Type = JsonObjectType.Array, Item = new JsonSchema { Type = JsonObjectType.Object } }, "Payload")(@event);

    [Fact] void should_keep_the_collection_as_a_list() => _result.ShouldBeOfExactType<List<object>>();
    [Fact] void should_preserve_the_dictionary_element() => ReferenceEquals(((List<object>)_result)[0], _dictionary).ShouldBeTrue();
    [Fact] void should_preserve_the_scalar_element() => ((List<object>)_result)[1].ShouldEqual(42);
    [Fact] void should_preserve_the_null_element() => ((List<object>)_result)[2].ShouldBeNull();
    [Fact] void should_preserve_the_element_count() => ((List<object>)_result).Count.ShouldEqual(3);
    [Fact] void should_not_change_the_input_collection() => _input.SequenceEqual([_dictionary, 42, null]).ShouldBeTrue();
    [Fact] void should_not_change_the_dictionary() => _dictionary.SequenceEqual(new Dictionary<object, object> { ["Name"] = "Upper", ["name"] = "Lower", ["null"] = null! }).ShouldBeTrue();
}
