// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Testing.ReadModels.for_ProjectionReadModelProcessor.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ProjectionReadModelProcessor.when_ensuring_a_child_collection;

public class and_the_parent_is_addressed_by_identifier : Specification
{
    ExpandoObject _state;
    ExpandoObject _secondGroup;
    List<object> _result;

    void Establish()
    {
        _secondGroup = StateBuilder.Expando(("id", 2), ("items", new List<object> { StateBuilder.Expando(("id", 20)) }));
        _state = StateBuilder.Expando(("groups", new List<object> { StateBuilder.Expando(("id", 1)), _secondGroup }));
    }

    void Because() => _result = ProjectionReadModelProcessor.EnsureChildCollection(
        _state,
        new ChildAdded(
            new ExpandoObject(),
            new PropertyPath("groups.items"),
            new PropertyPath("id"),
            21,
            new ArrayIndexers([new ArrayIndexer(new PropertyPath("groups"), new PropertyPath("id"), 2), new ArrayIndexer(new PropertyPath("groups.items"), new PropertyPath("id"), 21)])));

    [Fact] void should_return_the_items_of_the_matching_parent() => _result.Count.ShouldEqual(1);
    [Fact] void should_store_the_returned_collection_on_the_matching_parent() => StateBuilder.Get(_secondGroup, "items").ShouldEqual(_result);
    [Fact] void should_not_add_a_parent() => StateBuilder.Items(_state, "groups").Count.ShouldEqual(2);
}
