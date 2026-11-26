// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectExtensions.when_ensuring_an_array_path;

public class and_the_array_does_not_exist : Specification
{
    ExpandoObject _state;
    ExpandoObject _child;

    void Establish() => _state = new ExpandoObject();

    void Because() => _child = _state.EnsurePath(
        new PropertyPath("[children].topic"),
        new ArrayIndexers([new ArrayIndexer("[children]", "id", "A")]));

    [Fact] void should_create_the_collection() => (((IDictionary<string, object?>)_state)["children"] is IEnumerable<ExpandoObject>).ShouldBeTrue();
    [Fact] void should_add_one_child() => ((IEnumerable<ExpandoObject>)((IDictionary<string, object?>)_state)["children"]!).Count().ShouldEqual(1);
    [Fact] void should_return_the_added_child() => ((IEnumerable<ExpandoObject>)((IDictionary<string, object?>)_state)["children"]!).Single().ShouldEqual(_child);
    [Fact] void should_set_the_identity() => ((IDictionary<string, object?>)_child)["id"].ShouldEqual("A");
}
