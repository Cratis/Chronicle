// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Testing.ReadModels.for_ProjectionReadModelProcessor.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ProjectionReadModelProcessor.when_injecting_an_identifier_from_a_key;

public class and_the_identifier_is_already_mapped : Specification
{
    ExpandoObject _state;

    void Establish() => _state = StateBuilder.Expando(("id", "mapped"));

    void Because() => ProjectionReadModelProcessor.InjectIdentifierFromKey(_state, new(Guid.NewGuid(), ArrayIndexers.NoIndexers), typeof(Shelf));

    [Fact] void should_keep_the_mapped_value() => StateBuilder.Get(_state, "id").ShouldEqual("mapped");
}
