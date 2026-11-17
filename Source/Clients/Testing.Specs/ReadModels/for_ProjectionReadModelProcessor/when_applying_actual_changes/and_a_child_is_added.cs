// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Testing.ReadModels.for_ProjectionReadModelProcessor.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ProjectionReadModelProcessor.when_applying_actual_changes;

public class and_a_child_is_added : Specification
{
    ExpandoObject _state;
    ExpandoObject _child;
    ExpandoObject _result;

    void Establish()
    {
        _state = StateBuilder.Expando(("name", "x"));
        _child = StateBuilder.Expando(("title", "child"));
    }

    void Because() => _result = ProjectionReadModelProcessor.ApplyActualChanges(
        new(42, ArrayIndexers.NoIndexers),
        [new ChildAdded(_child, new PropertyPath("items"), new PropertyPath("id"), 5, ArrayIndexers.NoIndexers)],
        _state);

    [Fact] void should_return_the_state() => _result.ShouldEqual(_state);
    [Fact] void should_add_the_child() => StateBuilder.Items(_state, "items").Single().ShouldEqual(_child);
    [Fact] void should_identify_the_child_by_its_key() => StateBuilder.Get(_child, "id").ShouldEqual(5);
}
