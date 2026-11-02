// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Registrations;

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers;

public class when_registering_with_a_legacy_program_identifier : given.a_registration_stream
{
    void Establish() => _registration.ProgramIdentifier = ProgramIdentifier.Unknown.Value;

    Task Because() => Register();

    [Fact] void should_subscribe_with_an_unknown_owner() => _definition.ProgramIdentifier.ShouldEqual(ProgramIdentifier.Unknown);
}
