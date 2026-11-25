// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Testing.ReadModels.for_ProjectionReadModelProcessor.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ProjectionReadModelProcessor.when_removing_from_a_collection;

public class and_the_item_is_not_an_object : Specification
{
    ExpandoObject _state;

    void Establish() => _state = StateBuilder.Expando(("items", new List<object> { "plain", StateBuilder.Expando(("id", 1)) }));

    void Because() => ProjectionReadModelProcessor.RemoveFromCollection(_state, ["items"], "id", 1);

    [Fact] void should_keep_the_plain_item() => StateBuilder.Items(_state, "items").Single().ShouldEqual("plain");
}
