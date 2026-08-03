// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectExtensions;

public class when_cloning_expando_object_with_a_dictionary_property : Specification
{
    ExpandoObject _original;
    IDictionary<string, object?> _clone;
    Dictionary<object, object> _dictionary;

    void Establish()
    {
        _dictionary = new Dictionary<object, object>
        {
            ["first"] = "firstValue",
            ["second"] = "secondValue"
        };

        _original = new();
        dynamic asDynamic = _original;
        asDynamic.Entries = _dictionary;
    }

    void Because() => _clone = _original.Clone();

    [Fact] void should_preserve_the_dictionary_type() => _clone["Entries"].ShouldBeOfExactType<Dictionary<object, object>>();
    [Fact] void should_preserve_the_first_entry() => ((Dictionary<object, object>)_clone["Entries"]!)["first"].ShouldEqual("firstValue");
    [Fact] void should_preserve_the_second_entry() => ((Dictionary<object, object>)_clone["Entries"]!)["second"].ShouldEqual("secondValue");
    [Fact] void should_not_be_the_same_dictionary_instance() => ReferenceEquals(_clone["Entries"], _dictionary).ShouldBeFalse();
}
