// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectExtensions.when_ensuring_an_array_path;

public class and_the_identity_is_a_positional_index : Specification
{
    ExpandoObject _state;
    ExpandoObject _first;
    ExpandoObject _second;
    ExpandoObject _child;

    void Establish()
    {
        _first = new ExpandoObject();
        _second = new ExpandoObject();
        _state = new ExpandoObject();
        ((IDictionary<string, object?>)_state)["children"] = new List<ExpandoObject> { _first, _second };
    }

    void Because() => _child = _state.EnsurePath(
        new PropertyPath("[children].topic"),
        new ArrayIndexers([new ArrayIndexer("[children]", PropertyPath.NotSet, 1)]));

    [Fact] void should_return_the_child_at_the_index() => _child.ShouldEqual(_second);
    [Fact] void should_not_add_a_child() => ((IEnumerable<ExpandoObject>)((IDictionary<string, object?>)_state)["children"]!).Count().ShouldEqual(2);
}
