// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Testing.ReadModels.for_ProjectionReadModelProcessor.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ProjectionReadModelProcessor.when_injecting_an_identifier_from_a_key;

public class and_the_read_model_has_no_identifier : Specification
{
    ExpandoObject _state;

    void Establish() => _state = StateBuilder.Expando(("name", "x"));

    void Because() => ProjectionReadModelProcessor.InjectIdentifierFromKey(_state, new(Guid.NewGuid(), ArrayIndexers.NoIndexers), typeof(NoIdentifierModel));

    [Fact] void should_not_write_an_identifier() => ((IDictionary<string, object?>)_state).Count.ShouldEqual(1);

    record NoIdentifierModel(string Name);
}
