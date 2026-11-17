// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectExtensions.when_ensuring_a_collection;

public class and_the_property_is_a_sequence : Specification
{
    ExpandoObject _state;
    ICollection<string> _collection;

    void Establish()
    {
        _state = new ExpandoObject();
        ((IDictionary<string, object?>)_state)["items"] = new[] { "a", "b" }.Where(_ => true);
    }

    void Because() => _collection = _state.EnsureCollection<string>("items", ArrayIndexers.NoIndexers);

    [Fact] void should_return_a_list_with_the_items() => _collection.ShouldContainOnly("a", "b");
    [Fact] void should_store_the_list() => ReferenceEquals(((IDictionary<string, object?>)_state)["items"], _collection).ShouldBeTrue();
}
