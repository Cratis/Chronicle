// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Testing.ReadModels.for_ProjectionReadModelProcessor.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ProjectionReadModelProcessor.when_removing_from_a_collection;

public class and_the_item_is_in_a_leaf_collection : Specification
{
    ExpandoObject _state;

    void Establish() => _state = StateBuilder.Expando(
        ("items", new List<object>
        {
            StateBuilder.Expando(("id", 1), ("name", "one")),
            StateBuilder.Expando(("id", 2), ("name", "two"))
        }));

    void Because() => ProjectionReadModelProcessor.RemoveFromCollection(_state, ["items"], "id", 1);

    [Fact] void should_keep_only_the_other_item() => StateBuilder.Items(_state, "items").Count.ShouldEqual(1);
    [Fact] void should_keep_the_item_with_the_other_identifier() => StateBuilder.Get(StateBuilder.Items(_state, "items")[0], "id").ShouldEqual(2);
}
