// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Testing.ReadModels.for_ProjectionReadModelProcessor.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ProjectionReadModelProcessor.when_applying_actual_changes;

public class and_changes_are_joined : Specification
{
    ExpandoObject _state;

    void Establish() => _state = StateBuilder.Expando(("address", StateBuilder.Expando(("city", "Oslo"))));

    void Because() => ProjectionReadModelProcessor.ApplyActualChanges(
        new(42, ArrayIndexers.NoIndexers),
        [new Joined(new object(), 1, new PropertyPath("id"), ArrayIndexers.NoIndexers, [new NestedCleared(new PropertyPath("address"), ArrayIndexers.NoIndexers)])],
        _state);

    [Fact] void should_apply_the_joined_changes() => StateBuilder.Get(_state, "address").ShouldBeNull();
}
