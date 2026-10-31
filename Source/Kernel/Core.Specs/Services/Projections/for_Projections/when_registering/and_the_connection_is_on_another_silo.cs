// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Services.Projections.for_Projections.when_registering;

public class and_the_connection_is_on_another_silo : given.a_registration_with_a_connection
{
    void Establish() => _remoteClients.IsConnected(_connection).Returns(true);
    async Task Because() => await _service.Register(_request);

    [Fact] async Task should_record_on_the_connections_silo() => await _remoteClients.Received(1).RecordDeclaredProjectionVersions(_connection, EventStore, Arg.Is<IEnumerable<ProjectionId>>(ids => ids.Single() == _projectionDefinition.Identifier), RegisteredVersion);
    [Fact] async Task should_not_record_on_the_local_silo() => await _localClients.DidNotReceive().RecordDeclaredProjectionVersions(Arg.Any<ConnectionId>(), Arg.Any<EventStoreName>(), Arg.Any<IEnumerable<ProjectionId>>(), Arg.Any<string?>());
}
