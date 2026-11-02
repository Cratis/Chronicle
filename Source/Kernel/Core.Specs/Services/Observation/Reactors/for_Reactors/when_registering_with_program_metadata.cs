// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Registrations;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors;

public class when_registering_with_program_metadata : given.a_registration_stream
{
    void Establish() => _registration.ProgramIdentifier = "orders";

    Task Because() => Register();

    [Fact] void should_pass_the_program_identifier_to_the_definition() => _definition.ProgramIdentifier.ShouldEqual(new ProgramIdentifier("orders"));
}
