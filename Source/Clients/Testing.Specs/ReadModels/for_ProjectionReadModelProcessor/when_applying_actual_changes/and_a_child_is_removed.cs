// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Testing.ReadModels.for_ProjectionReadModelProcessor.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ProjectionReadModelProcessor.when_applying_actual_changes;

public class and_a_child_is_removed : Specification
{
    ExpandoObject _state;

    void Establish() => _state = StateBuilder.Expando(
        ("items", new List<object> { StateBuilder.Expando(("id", 1)), StateBuilder.Expando(("id", 2)) }));

    void Because() => ProjectionReadModelProcessor.ApplyActualChanges(
        new(42, ArrayIndexers.NoIndexers),
        [new ChildRemoved(new object(), new PropertyPath("items"), new PropertyPath("id"), 1)],
        _state);

    [Fact] void should_remove_the_child() => StateBuilder.Items(_state, "items").Count.ShouldEqual(1);
    [Fact] void should_keep_the_other_child() => StateBuilder.Get(StateBuilder.Items(_state, "items")[0], "id").ShouldEqual(2);
}
