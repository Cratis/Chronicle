// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Services.Projections.for_Projections.when_registering;

public class with_real_ownership_a_connection_was_released : given.a_registration_with_a_connection
{
    readonly ConnectionOwnership _real = new();

    void Establish()
    {
        _localClients.IsConnected(_connection).Returns(true);
        _ownership.IsOwnedBy(Arg.Any<string>(), Arg.Any<ConnectionOwner>()).Returns(call => _real.IsOwnedBy(call.Arg<string>(), call.Arg<ConnectionOwner>()));
        _real.Claim(_connection, AuthenticatedCalls.OwnerOf("alice"));
        _real.Release(_connection, AuthenticatedCalls.OwnerOf("alice"));
    }

    async Task Because() => await _service.Register(_request, AuthenticatedCalls.As("alice"));

    [Fact] async Task should_not_record_on_the_local_silo() => await _localClients.DidNotReceive().RecordDeclaredProjectionVersions(Arg.Any<ConnectionId>(), Arg.Any<EventStoreName>(), Arg.Any<IEnumerable<ProjectionId>>(), Arg.Any<string?>());
}
