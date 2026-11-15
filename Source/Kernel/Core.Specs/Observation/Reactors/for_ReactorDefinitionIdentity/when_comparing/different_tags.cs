// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reactors.for_ReactorDefinitionIdentity.when_comparing;

public class different_tags : given.a_stored_and_an_incoming_reactor_definition
{
    void Establish() => _incoming = _incoming with { Tags = ["other-tag"] };

    void Because() => _result = ReactorDefinitionIdentity.IsIdentical(_stored, _incoming);

    [Fact] void should_not_be_identical() => _result.ShouldBeFalse();
}
