// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Testing.ReadModels.for_ProjectionReadModelProcessor.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ProjectionReadModelProcessor.when_injecting_an_identifier_from_a_key;

public class and_the_identifier_is_mapped_to_null : Specification
{
    ExpandoObject _state;
    Guid _key;

    void Establish()
    {
        _key = Guid.NewGuid();
        _state = StateBuilder.Expando(("id", null));
    }

    void Because() => ProjectionReadModelProcessor.InjectIdentifierFromKey(_state, new(_key, ArrayIndexers.NoIndexers), typeof(Shelf));

    [Fact] void should_write_the_key() => StateBuilder.Get(_state, "id").ShouldEqual(_key);
}
