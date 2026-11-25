// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Testing.ReadModels.for_ProjectionReadModelProcessor.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ProjectionReadModelProcessor.when_injecting_a_child_identifier;

public class and_the_identifier_is_set : Specification
{
    ExpandoObject _child;

    void Establish() => _child = StateBuilder.Expando(("name", "child"));

    void Because() => ProjectionReadModelProcessor.InjectChildIdentifierFromKey(
        new ChildAdded(_child, new PropertyPath("items"), new PropertyPath("id"), 42, ArrayIndexers.NoIndexers));

    [Fact] void should_write_the_key_on_the_child() => StateBuilder.Get(_child, "id").ShouldEqual(42);
}
