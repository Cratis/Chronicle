// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Frozen;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_EventValueProviderExpressionResolvers;

public class when_resolving_a_frozen_dictionary : given.an_appended_event
{
    EventValueProviderExpressionResolvers _resolvers;
    FrozenDictionary<string, object?> _input;
    Dictionary<string, object?> _nested;
    KeyValuePair<string, object?>[] _originalEntries;
    KeyValuePair<string, object?>[] _originalNestedEntries;
    object _result;

    void Establish()
    {
        _resolvers = new(new TypeFormats(), Substitute.For<ILogger<EventValueProviderExpressionResolvers>>());
        _nested = new(StringComparer.Ordinal) { ["value"] = "42", ["null"] = null };
        _input = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Name"] = "Upper",
            ["name"] = "Lower",
            ["nested"] = _nested,
            ["null"] = null
        }.ToFrozenDictionary(StringComparer.Ordinal);
        _originalEntries = [.. _input];
        _originalNestedEntries = [.. _nested];
        ((IDictionary<string, object?>)@event.Content)["Payload"] = _input;
    }

    void Because() => _result = _resolvers.Resolve(new JsonSchemaProperty { Type = JsonObjectType.Object }, "Payload")(@event);

    [Fact] void should_exercise_the_generic_dictionary_contract() => _input.GetType().GetInterfaces().ShouldContain(typeof(IDictionary<string, object?>));
    [Fact] void should_exercise_the_non_generic_dictionary_contract() => _input.GetType().GetInterfaces().ShouldContain(typeof(IDictionary));
    [Fact] void should_preserve_the_dictionary_type() => _result.GetType().ShouldEqual(_input.GetType());
    [Fact] void should_preserve_the_dictionary_reference() => ReferenceEquals(_result, _input).ShouldBeTrue();
    [Fact] void should_preserve_all_entries() => ((FrozenDictionary<string, object?>)_result).SequenceEqual(_originalEntries).ShouldBeTrue();
    [Fact] void should_preserve_the_uppercase_key() => ((FrozenDictionary<string, object?>)_result)["Name"].ShouldEqual("Upper");
    [Fact] void should_preserve_the_lowercase_key() => ((FrozenDictionary<string, object?>)_result)["name"].ShouldEqual("Lower");
    [Fact] void should_preserve_the_null() => ((FrozenDictionary<string, object?>)_result)["null"].ShouldBeNull();
    [Fact] void should_preserve_the_nested_dictionary_reference() => ReferenceEquals(((FrozenDictionary<string, object?>)_result)["nested"], _nested).ShouldBeTrue();
    [Fact] void should_not_change_the_input() => _input.SequenceEqual(_originalEntries).ShouldBeTrue();
    [Fact] void should_not_change_nested_values() => _nested.SequenceEqual(_originalNestedEntries).ShouldBeTrue();
}
