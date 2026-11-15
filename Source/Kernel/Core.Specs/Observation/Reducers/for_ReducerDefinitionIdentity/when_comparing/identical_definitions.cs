// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerDefinitionIdentity.when_comparing;

public class identical_definitions : given.a_stored_and_an_incoming_reducer_definition
{
    void Because() => _result = ReducerDefinitionIdentity.IsIdentical(_stored, _incoming);

    [Fact] void should_be_identical() => _result.ShouldBeTrue();
}
