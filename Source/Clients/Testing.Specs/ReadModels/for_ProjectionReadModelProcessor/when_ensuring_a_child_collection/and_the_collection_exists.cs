// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Testing.ReadModels.for_ProjectionReadModelProcessor.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ProjectionReadModelProcessor.when_ensuring_a_child_collection;

public class and_the_collection_exists : Specification
{
    ExpandoObject _state;
    List<object> _result;

    void Establish() => _state = StateBuilder.Expando(("items", new List<object> { StateBuilder.Expando(("id", 1)) }));

    void Because() => _result = ProjectionReadModelProcessor.EnsureChildCollection(
        _state,
        new ChildAdded(new ExpandoObject(), new PropertyPath("items"), new PropertyPath("id"), 2, ArrayIndexers.NoIndexers));

    [Fact] void should_return_the_existing_items() => _result.Count.ShouldEqual(1);
    [Fact] void should_store_the_returned_collection_on_the_state() => StateBuilder.Get(_state, "items").ShouldEqual(_result);
}
