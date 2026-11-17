// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectExtensions.when_ensuring_an_array_path;

public class and_the_positional_index_is_out_of_range : Specification
{
    ExpandoObject _state;
    ExpandoObject _child;

    void Establish()
    {
        _state = new ExpandoObject();
        ((IDictionary<string, object?>)_state)["children"] = new List<ExpandoObject>();
    }

    void Because() => _child = _state.EnsurePath(
        new PropertyPath("[children].topic"),
        new ArrayIndexers([new ArrayIndexer("[children]", PropertyPath.NotSet, 3)]));

    [Fact] void should_append_a_child() => ((IEnumerable<ExpandoObject>)((IDictionary<string, object?>)_state)["children"]!).Single().ShouldEqual(_child);
}
