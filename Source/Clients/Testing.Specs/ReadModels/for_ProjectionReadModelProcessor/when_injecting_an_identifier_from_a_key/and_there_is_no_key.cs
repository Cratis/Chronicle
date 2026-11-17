// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Testing.ReadModels.for_ProjectionReadModelProcessor.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ProjectionReadModelProcessor.when_injecting_an_identifier_from_a_key;

public class and_there_is_no_key : Specification
{
    ExpandoObject _state;

    void Establish() => _state = StateBuilder.Expando(("name", "x"));

    void Because() => ProjectionReadModelProcessor.InjectIdentifierFromKey(_state, null, typeof(Shelf));

    [Fact] void should_not_write_an_identifier() => ((IDictionary<string, object?>)_state).ContainsKey("id").ShouldBeFalse();
}
