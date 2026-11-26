// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Testing.ReadModels.for_ProjectionReadModelProcessor.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ProjectionReadModelProcessor.when_removing_from_a_collection;

public class and_the_value_is_not_a_collection : Specification
{
    ExpandoObject _state;

    void Establish() => _state = StateBuilder.Expando(("items", 5));

    void Because() => ProjectionReadModelProcessor.RemoveFromCollection(_state, ["items"], "id", 1);

    [Fact] void should_leave_the_value_untouched() => StateBuilder.Get(_state, "items").ShouldEqual(5);
}
