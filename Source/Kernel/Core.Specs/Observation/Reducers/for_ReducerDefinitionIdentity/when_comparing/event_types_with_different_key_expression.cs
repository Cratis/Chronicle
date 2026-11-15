// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerDefinitionIdentity.when_comparing;

public class event_types_with_different_key_expression : given.a_stored_and_an_incoming_reducer_definition
{
    void Establish() => _incoming = _incoming with { EventTypes = _incoming.EventTypes.Select(_ => _ with { Key = new("other-key") }).ToArray() };

    void Because() => _result = ReducerDefinitionIdentity.IsIdentical(_stored, _incoming);

    [Fact] void should_should_not_be_identical() => _result.ShouldBeFalse();
}
