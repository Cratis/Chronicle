// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;

namespace Cratis.Chronicle.Services.Projections.for_Projections.when_registering;

public class while_connection_ownership_cannot_be_verified : given.a_registration_with_a_connection
{
    Exception _error;
    protected override IConnectionOwnership CreateOwnership() => new ConnectionOwnership();
    void Establish()
    {
        var reservation = Substitute.For<IConnectionReservation>();
        reservation.IsHeldBy(AuthenticatedCalls.OwnerOf("alice")).Returns(new TaskCompletionSource<bool>().Task);
        _grainFactory.GetGrain<IConnectionReservation>(Arg.Any<string>(), Arg.Any<string>()).Returns(reservation);
    }
    async Task Because() => _error = await Catch.Exception(() => _service.Register(_request, AuthenticatedCalls.As("alice")));

    [Fact] void should_still_register() => _error.ShouldBeNull();
    [Fact] async Task should_register_definitions() => await _manager.Received(1).RegisterShared(Arg.Any<string>(), Arg.Any<IEnumerable<Concepts.Projections.Definitions.ProjectionDefinition>>(), null, null, RegisteredVersion);
    [Fact] void should_not_record_declarations() => _localClients.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_record_remote_declarations() => _remoteClients.ReceivedCalls().ShouldBeEmpty();
}
