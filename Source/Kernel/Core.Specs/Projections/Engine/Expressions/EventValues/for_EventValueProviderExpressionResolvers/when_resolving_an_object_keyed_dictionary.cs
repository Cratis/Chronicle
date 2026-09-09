// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_EventValueProviderExpressionResolvers;

public class when_resolving_an_object_keyed_dictionary : given.an_appended_event
{
    EventValueProviderExpressionResolvers _resolvers;
    Dictionary<object, object> _input;
    Dictionary<string, object?> _nested;
    object?[] _values;
    KeyValuePair<object, object>[] _originalEntries;
    object _result;

    void Establish()
    {
        _resolvers = new(new TypeFormats(), Substitute.For<ILogger<EventValueProviderExpressionResolvers>>());
        _nested = new(StringComparer.Ordinal) { ["Name"] = "Nested upper", ["name"] = "Nested lower", ["null"] = null };
        _values = ["value", 42, null];
        _input = new()
        {
            ["Name"] = "Upper",
            ["name"] = "Lower",
            [42] = "Non-string key",
            ["nested"] = _nested,
            ["values"] = _values,
            ["null"] = null!
        };
        _originalEntries = [.. _input];
        ((IDictionary<string, object?>)@event.Content)["Payload"] = _input;
    }

    void Because() => _result = _resolvers.Resolve(new JsonSchemaProperty { Type = JsonObjectType.Object }, "Payload")(@event);

    [Fact] void should_preserve_the_dictionary_type() => _result.ShouldBeOfExactType<Dictionary<object, object>>();
    [Fact] void should_preserve_the_dictionary_reference() => ReferenceEquals(_result, _input).ShouldBeTrue();
    [Fact] void should_preserve_all_entries() => ((Dictionary<object, object>)_result).SequenceEqual(_originalEntries).ShouldBeTrue();
    [Fact] void should_preserve_the_uppercase_key() => ((Dictionary<object, object>)_result)["Name"].ShouldEqual("Upper");
    [Fact] void should_preserve_the_lowercase_key() => ((Dictionary<object, object>)_result)["name"].ShouldEqual("Lower");
    [Fact] void should_preserve_the_non_string_key() => ((Dictionary<object, object>)_result)[42].ShouldEqual("Non-string key");
    [Fact] void should_preserve_the_null() => ((Dictionary<object, object>)_result)["null"].ShouldBeNull();
    [Fact] void should_not_change_the_input() => _input.SequenceEqual(_originalEntries).ShouldBeTrue();
    [Fact] void should_not_change_nested_values() => _nested.SequenceEqual(new Dictionary<string, object?> { ["Name"] = "Nested upper", ["name"] = "Nested lower", ["null"] = null }).ShouldBeTrue();
    [Fact] void should_not_change_the_nested_array() => _values.SequenceEqual(["value", 42, null]).ShouldBeTrue();
}
