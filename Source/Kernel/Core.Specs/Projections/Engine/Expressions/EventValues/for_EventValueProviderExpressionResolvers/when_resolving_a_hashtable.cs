// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_EventValueProviderExpressionResolvers;

public class when_resolving_a_hashtable : given.an_appended_event
{
    EventValueProviderExpressionResolvers _resolvers;
    Hashtable _input;
    Hashtable _nested;
    DictionaryEntry[] _originalEntries;
    DictionaryEntry[] _originalNestedEntries;
    object _result;

    void Establish()
    {
        _resolvers = new(new TypeFormats(), Substitute.For<ILogger<EventValueProviderExpressionResolvers>>());
        _nested = new(StringComparer.Ordinal) { ["value"] = "42", ["null"] = null };
        _input = new(StringComparer.Ordinal)
        {
            ["Name"] = "Upper",
            ["name"] = "Lower",
            [42] = "Non-string key",
            ["nested"] = _nested,
            ["null"] = null
        };
        _originalEntries = [.. _input.Cast<DictionaryEntry>()];
        _originalNestedEntries = [.. _nested.Cast<DictionaryEntry>()];
        ((IDictionary<string, object?>)@event.Content)["Payload"] = _input;
    }

    void Because() => _result = _resolvers.Resolve(new JsonSchemaProperty { Type = JsonObjectType.Object }, "Payload")(@event);

    [Fact] void should_preserve_the_dictionary_type() => _result.ShouldBeOfExactType<Hashtable>();
    [Fact] void should_preserve_the_dictionary_reference() => ReferenceEquals(_result, _input).ShouldBeTrue();
    [Fact] void should_preserve_all_entries() => ((Hashtable)_result).Cast<DictionaryEntry>().SequenceEqual(_originalEntries).ShouldBeTrue();
    [Fact] void should_preserve_the_uppercase_key() => ((Hashtable)_result)["Name"].ShouldEqual("Upper");
    [Fact] void should_preserve_the_lowercase_key() => ((Hashtable)_result)["name"].ShouldEqual("Lower");
    [Fact] void should_preserve_the_non_string_key() => ((Hashtable)_result)[42].ShouldEqual("Non-string key");
    [Fact] void should_preserve_the_null() => ((Hashtable)_result)["null"].ShouldBeNull();
    [Fact] void should_preserve_the_nested_dictionary_reference() => ReferenceEquals(((Hashtable)_result)["nested"], _nested).ShouldBeTrue();
    [Fact] void should_preserve_the_nested_null() => ((Hashtable)((Hashtable)_result)["nested"]!)["null"].ShouldBeNull();
    [Fact] void should_not_change_the_input() => _input.Cast<DictionaryEntry>().SequenceEqual(_originalEntries).ShouldBeTrue();
    [Fact] void should_not_change_nested_values() => _nested.Cast<DictionaryEntry>().SequenceEqual(_originalNestedEntries).ShouldBeTrue();
}
