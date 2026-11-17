// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectExtensions.when_ensuring_a_collection;

public class and_the_property_is_not_enumerable : Specification
{
    ExpandoObject _state;
    Exception _exception;

    void Establish()
    {
        _state = new ExpandoObject();
        ((IDictionary<string, object?>)_state)["items"] = 42;
    }

    void Because() => _exception = Catch.Exception(() => _state.EnsureCollection<string>("items", ArrayIndexers.NoIndexers));

    [Fact] void should_throw_children_property_is_not_enumerable() => _exception.ShouldBeOfExactType<ChildrenPropertyIsNotEnumerable>();
    [Fact] void should_have_message() => _exception.Message.ShouldEqual("Property at 'items' is not of enumerable type.");
}
