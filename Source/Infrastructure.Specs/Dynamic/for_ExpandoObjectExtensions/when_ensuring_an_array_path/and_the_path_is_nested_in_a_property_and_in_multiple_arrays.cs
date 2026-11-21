// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectExtensions.when_ensuring_an_array_path;

public class and_the_path_is_nested_in_a_property_and_in_multiple_arrays : Specification
{
    ExpandoObject _state;
    ExpandoObject _child;

    void Establish() => _state = new ExpandoObject();

    void Because() => _child = _state.EnsurePath(
        new PropertyPath("outer.[groups].[items].name"),
        new ArrayIndexers(
        [
            new ArrayIndexer("outer.[groups]", "id", "G"),
            new ArrayIndexer("outer.[groups].[items]", "id", "I")
        ]));

    [Fact] void should_create_the_outer_object() => ((IDictionary<string, object?>)_state)["outer"].ShouldBeOfExactType<ExpandoObject>();
    [Fact] void should_set_item_identity() => ((IDictionary<string, object?>)_child)["id"].ShouldEqual("I");
    [Fact] void should_nest_item_in_group() => ((IDictionary<string, object?>)((IEnumerable<ExpandoObject>)((IDictionary<string, object?>)((IDictionary<string, object?>)_state)["outer"]!)["groups"]!).Single())["items"].ShouldNotBeNull();
}
