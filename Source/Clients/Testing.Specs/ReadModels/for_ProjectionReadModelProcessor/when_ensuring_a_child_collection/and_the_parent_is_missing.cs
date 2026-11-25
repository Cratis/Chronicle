// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Testing.ReadModels.for_ProjectionReadModelProcessor.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ProjectionReadModelProcessor.when_ensuring_a_child_collection;

public class and_the_parent_is_missing : Specification
{
    ExpandoObject _state;
    List<object> _result;

    void Establish() => _state = StateBuilder.Expando(("name", "x"));

    void Because() => _result = ProjectionReadModelProcessor.EnsureChildCollection(
        _state,
        new ChildAdded(
            new ExpandoObject(),
            new PropertyPath("groups.items"),
            new PropertyPath("id"),
            21,
            new ArrayIndexers([new ArrayIndexer(new PropertyPath("groups"), new PropertyPath("id"), 7), new ArrayIndexer(new PropertyPath("groups.items"), new PropertyPath("id"), 21)])));

    [Fact] void should_create_the_parent() => StateBuilder.Items(_state, "groups").Count.ShouldEqual(1);
    [Fact] void should_identify_the_created_parent() => StateBuilder.Get(StateBuilder.Items(_state, "groups")[0], "id").ShouldEqual(7);
    [Fact] void should_return_an_empty_collection_on_the_created_parent() => StateBuilder.Get(StateBuilder.Items(_state, "groups")[0], "items").ShouldEqual(_result);
}
