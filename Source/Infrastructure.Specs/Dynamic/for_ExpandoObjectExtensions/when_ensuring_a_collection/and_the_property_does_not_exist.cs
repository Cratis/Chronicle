// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectExtensions.when_ensuring_a_collection;

public class and_the_property_does_not_exist : Specification
{
    ExpandoObject _state;
    ICollection<string> _collection;

    void Establish() => _state = new ExpandoObject();

    void Because() => _collection = _state.EnsureCollection<string>("parent.items", ArrayIndexers.NoIndexers);

    [Fact] void should_return_an_empty_collection() => _collection.Count.ShouldEqual(0);
    [Fact] void should_store_the_collection() => ReferenceEquals(((IDictionary<string, object?>)((IDictionary<string, object?>)_state)["parent"]!)["items"], _collection).ShouldBeTrue();
}
