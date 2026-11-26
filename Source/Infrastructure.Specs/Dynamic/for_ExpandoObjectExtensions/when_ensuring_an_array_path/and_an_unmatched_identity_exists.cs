// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectExtensions.when_ensuring_an_array_path;

public class and_an_unmatched_identity_exists : Specification
{
    ExpandoObject _state;
    ExpandoObject _existing;
    ExpandoObject _child;

    void Establish()
    {
        _existing = new ExpandoObject();
        ((IDictionary<string, object?>)_existing)["id"] = "A";
        _state = new ExpandoObject();
        ((IDictionary<string, object?>)_state)["children"] = new List<ExpandoObject> { _existing };
    }

    void Because() => _child = _state.EnsurePath(
        new PropertyPath("[children].topic"),
        new ArrayIndexers([new ArrayIndexer("[children]", "id", "B")]));

    [Fact] void should_append_a_child() => ((IEnumerable<ExpandoObject>)((IDictionary<string, object?>)_state)["children"]!).Count().ShouldEqual(2);
    [Fact] void should_keep_the_existing_first() => ((IEnumerable<ExpandoObject>)((IDictionary<string, object?>)_state)["children"]!).First().ShouldEqual(_existing);
    [Fact] void should_return_the_new_child() => ((IEnumerable<ExpandoObject>)((IDictionary<string, object?>)_state)["children"]!).Last().ShouldEqual(_child);
    [Fact] void should_set_the_identity_on_the_new_child() => ((IDictionary<string, object?>)_child)["id"].ShouldEqual("B");
}
