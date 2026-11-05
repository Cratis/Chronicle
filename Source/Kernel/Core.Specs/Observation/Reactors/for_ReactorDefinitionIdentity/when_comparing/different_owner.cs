// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Observation.Reactors.for_ReactorDefinitionIdentity.when_comparing;

public class different_owner : given.a_stored_and_an_incoming_reactor_definition
{
    void Establish() => _incoming = _incoming with { Owner = ReactorOwner.Kernel };

    void Because() => _result = ReactorDefinitionIdentity.IsIdentical(_stored, _incoming);

    [Fact] void should_not_be_identical() => _result.ShouldBeFalse();
}
