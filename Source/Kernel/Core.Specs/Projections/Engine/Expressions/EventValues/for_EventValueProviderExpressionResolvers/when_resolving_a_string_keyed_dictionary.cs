// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_EventValueProviderExpressionResolvers;

public class when_resolving_a_string_keyed_dictionary : given.an_appended_event
{
    EventValueProviderExpressionResolvers _resolvers;
    Dictionary<string, object?> _input;
    Dictionary<object, object> _nested;
    KeyValuePair<string, object?>[] _originalEntries;
    object _result;

    void Establish()
    {
        _resolvers = new(new TypeFormats(), Substitute.For<ILogger<EventValueProviderExpressionResolvers>>());
        _nested = new() { ["value"] = "42" };
        _input = new(StringComparer.Ordinal)
        {
            ["Name"] = "Upper",
            ["name"] = "Lower",
            ["nested"] = _nested,
            ["null"] = null
        };
        _originalEntries = [.. _input];
        ((IDictionary<string, object?>)@event.Content)["Payload"] = _input;
    }

    void Because() => _result = _resolvers.Resolve(new JsonSchemaProperty { Type = JsonObjectType.Object, Format = TypeFormats.DynamicFormat }, "Payload")(@event);

    [Fact] void should_preserve_the_dictionary_type() => _result.ShouldBeOfExactType<Dictionary<string, object?>>();
    [Fact] void should_preserve_the_dictionary_reference() => ReferenceEquals(_result, _input).ShouldBeTrue();
    [Fact] void should_preserve_all_entries() => ((Dictionary<string, object?>)_result).SequenceEqual(_originalEntries).ShouldBeTrue();
    [Fact] void should_preserve_the_uppercase_key() => ((Dictionary<string, object?>)_result)["Name"].ShouldEqual("Upper");
    [Fact] void should_preserve_the_lowercase_key() => ((Dictionary<string, object?>)_result)["name"].ShouldEqual("Lower");
    [Fact] void should_preserve_the_null() => ((Dictionary<string, object?>)_result)["null"].ShouldBeNull();
    [Fact] void should_not_change_the_input() => _input.SequenceEqual(_originalEntries).ShouldBeTrue();
    [Fact] void should_not_unwrap_a_nested_dictionary_as_a_concept() => _nested.SequenceEqual(new Dictionary<object, object> { ["value"] = "42" }).ShouldBeTrue();
}
