// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;

namespace Cratis.Chronicle.Services.Projections.for_Projections.when_registering;

public class with_the_session_on_another_silo : given.a_registration_with_a_connection
{
    readonly ConnectionOwnership _real = new();
    Exception _error;
    protected override IConnectionOwnership CreateOwnership() => _real;
    void Establish()
    {
        _grainFactory.GetGrain<IConnectionReservation>(Arg.Any<string>(), Arg.Any<string>()).Returns(_held);
        _remoteClients.IsConnected(_connection).Returns(true);
    }
    async Task Because() => _error = await Catch.Exception(() => _service.Register(_request, AuthenticatedCalls.As("alice")));

    [Fact] void should_register() => _error.ShouldBeNull();
    [Fact] async Task should_register_the_definitions() => await _manager.Received(1).RegisterShared(Arg.Any<string>(), Arg.Any<IEnumerable<Concepts.Projections.Definitions.ProjectionDefinition>>(), null, null, RegisteredVersion);
    [Fact] async Task should_record_the_declarations_on_the_holding_silo() => await _remoteClients.Received(1).RecordDeclaredProjectionVersions(_connection, EventStore, Arg.Any<IEnumerable<Concepts.Projections.ProjectionId>>(), RegisteredVersion);
    [Fact] void should_not_claim_the_connection_locally() => _real.IsOwnedBy(_connection.Value, AuthenticatedCalls.OwnerOf("alice")).ShouldBeFalse();
}
