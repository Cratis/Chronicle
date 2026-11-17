// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectExtensions.when_ensuring_a_collection;

public class and_the_property_is_a_list : Specification
{
    ExpandoObject _state;
    List<string> _list;
    ICollection<string> _collection;

    void Establish()
    {
        _list = ["a", "b"];
        _state = new ExpandoObject();
        ((IDictionary<string, object?>)_state)["items"] = _list;
    }

    void Because() => _collection = _state.EnsureCollection<string>("items", ArrayIndexers.NoIndexers);

    [Fact] void should_return_the_same_list() => ReferenceEquals(_collection, _list).ShouldBeTrue();
}
