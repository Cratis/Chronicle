// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_ChangesetStateNavigator;

public class when_getting_an_element_by_identifier : Specification
{
    object? _value;
    bool _result;

    void Because()
    {
        dynamic first = new ExpandoObject();
        first.id = "a";
        dynamic second = new ExpandoObject();
        second.id = "b";
        dynamic state = new ExpandoObject();
        state.items = new List<object> { (ExpandoObject)first, (ExpandoObject)second };
        _result = ChangesetStateNavigator.TryGetValueAtPath(
            (ExpandoObject)state,
            new PropertyPath("[items]"),
            new ArrayIndexers([new ArrayIndexer(new PropertyPath("[items]"), new PropertyPath("id"), "b")]),
            out _value);
    }

    [Fact] void should_find_the_value() => _result.ShouldBeTrue();
    [Fact] void should_return_the_element_with_the_identifier() => ((IDictionary<string, object?>)_value!)["id"].ShouldEqual("b");
}
