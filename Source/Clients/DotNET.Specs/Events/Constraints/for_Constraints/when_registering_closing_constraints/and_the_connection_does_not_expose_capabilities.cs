// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_Constraints.when_registering_closing_constraints;

public class and_the_connection_does_not_expose_capabilities : given.a_closing_constraint
{
    Task Because() => _constraints.Register();

    [Fact] void should_send_the_definitions_to_the_kernel() => _constraintsService.Received(1).Register(Arg.Any<RegisterConstraintsRequest>());
}
