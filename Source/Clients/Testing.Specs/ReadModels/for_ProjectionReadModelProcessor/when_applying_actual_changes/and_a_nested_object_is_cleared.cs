// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Testing.ReadModels.for_ProjectionReadModelProcessor.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ProjectionReadModelProcessor.when_applying_actual_changes;

public class and_a_nested_object_is_cleared : Specification
{
    ExpandoObject _state;

    void Establish() => _state = StateBuilder.Expando(("address", StateBuilder.Expando(("city", "Oslo"))));

    void Because() => ProjectionReadModelProcessor.ApplyActualChanges(
        new(42, ArrayIndexers.NoIndexers),
        [new NestedCleared(new PropertyPath("address"), ArrayIndexers.NoIndexers)],
        _state);

    [Fact] void should_set_the_nested_object_to_null() => StateBuilder.Get(_state, "address").ShouldBeNull();
}
