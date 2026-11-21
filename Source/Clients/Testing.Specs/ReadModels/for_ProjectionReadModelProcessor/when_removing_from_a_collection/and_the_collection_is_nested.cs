// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Testing.ReadModels.for_ProjectionReadModelProcessor.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ProjectionReadModelProcessor.when_removing_from_a_collection;

public class and_the_collection_is_nested : Specification
{
    ExpandoObject _state;

    void Establish() => _state = StateBuilder.Expando(
        ("groups", new List<object>
        {
            StateBuilder.Expando(("items", new List<object> { StateBuilder.Expando(("id", 1)), StateBuilder.Expando(("id", 2)) })),
            StateBuilder.Expando(("items", new List<object> { StateBuilder.Expando(("id", 1)) }))
        }));

    void Because() => ProjectionReadModelProcessor.RemoveFromCollection(_state, ["groups", "items"], "id", 1);

    [Fact] void should_remove_from_the_first_group() => StateBuilder.Items(StateBuilder.Items(_state, "groups")[0], "items").Count.ShouldEqual(1);
    [Fact] void should_remove_from_the_second_group() => StateBuilder.Items(StateBuilder.Items(_state, "groups")[1], "items").Count.ShouldEqual(0);
}
