// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reactors.for_ReactorDefinitionIdentity.when_comparing;

public class null_tags_against_empty_tags : given.a_stored_and_an_incoming_reactor_definition
{
    void Establish()
    {
        _stored = _stored with { Tags = null };
        _incoming = _incoming with { Tags = [] };
    }

    void Because() => _result = ReactorDefinitionIdentity.IsIdentical(_stored, _incoming);

    [Fact] void should_be_identical() => _result.ShouldBeTrue();
}
