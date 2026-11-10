// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;

namespace Cratis.Chronicle.Services.Projections.for_Projections.when_registering;

public class with_a_connection_another_principal_holds_on_another_silo : given.a_registration_with_a_connection
{
    Exception _error;
    protected override IConnectionOwnership CreateOwnership() => new ConnectionOwnership();
    void Establish() => _grainFactory.GetGrain<IConnectionReservation>(Arg.Any<string>(), Arg.Any<string>()).Returns(_held);
    async Task Because() => _error = await Catch.Exception(() => _service.Register(_request, AuthenticatedCalls.As("bob")));

    [Fact] void should_still_register() => _error.ShouldBeNull();
    [Fact] void should_not_record_declarations_locally() => _localClients.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_record_declarations_remotely() => _remoteClients.ReceivedCalls().ShouldBeEmpty();
}
