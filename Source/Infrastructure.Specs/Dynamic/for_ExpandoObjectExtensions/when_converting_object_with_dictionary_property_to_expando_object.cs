// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectExtensions;

public class when_converting_object_with_dictionary_property_to_expando_object : Specification
{
    Dictionary<string, string> _entries;
    IDictionary<string, object?> _result;

    void Establish() => _entries = new()
    {
        ["first"] = "firstValue",
        ["second"] = "secondValue"
    };

    void Because() => _result = new WithDictionaryProperty(_entries).AsExpandoObject(camelCaseProperties: true);

    [Fact] void should_keep_the_property_as_a_dictionary() => _result["entries"].ShouldBeOfExactType<Dictionary<object, object>>();
    [Fact] void should_keep_the_first_entry() => ((Dictionary<object, object>)_result["entries"]!)["first"].ShouldEqual("firstValue");
    [Fact] void should_keep_the_second_entry() => ((Dictionary<object, object>)_result["entries"]!)["second"].ShouldEqual("secondValue");
}
