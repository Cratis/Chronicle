// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Services.Projections.for_Projections.when_registering;

public class with_a_connection_another_principal_owns : given.a_registration_with_a_connection
{
    void Establish()
    {
        _localClients.IsConnected(_connection).Returns(true);
        _ownership.IsOwnedBy(Arg.Any<string>(), Arg.Any<Cratis.Chronicle.Clients.ConnectionOwner>()).Returns(false);
    }
    async Task Because() => await _service.Register(_request);

    [Fact] async Task should_not_record_on_the_local_silo() => await _localClients.DidNotReceive().RecordDeclaredProjectionVersions(Arg.Any<ConnectionId>(), Arg.Any<EventStoreName>(), Arg.Any<IEnumerable<ProjectionId>>(), Arg.Any<string?>());
    [Fact] async Task should_still_register_the_projections() => await _manager.Received(1).RegisterShared(Arg.Any<string>(), Arg.Any<IEnumerable<Concepts.Projections.Definitions.ProjectionDefinition>>(), null, null, RegisteredVersion);
}
