// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_Constraints.when_registering_closing_constraints;

public class and_the_connection_does_not_expose_capabilities : given.a_closing_constraint
{
    async Task Because() => _error = await Catch.Exception(_constraints.Register);

    [Fact] void should_refuse_the_unsupported_definitions() => _error.ShouldBeOfExactType<ClosesStreamConstraintsNotSupported>();
    [Fact] void should_send_nothing() => _constraintsService.DidNotReceive().Register(Arg.Any<RegisterConstraintsRequest>());
}
