// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

public class and_the_connection_is_on_another_silo : given.a_manager_with_an_orphaned_projection
{
    readonly ConnectionId _connection = ConnectionId.New();

    void Establish() => _remoteClients.IsConnected(_connection).Returns(true);
    async Task Because() => await _grain.Register([_definition], connectionId: _connection);

    [Fact] async Task should_record_on_the_connections_silo() => await _remoteClients.Received(1).RecordDeclaredProjections(_connection, EventStore, Arg.Is<IEnumerable<ProjectionId>>(ids => ids.Single() == _definition.Identifier));
    [Fact] async Task should_not_record_on_the_local_silo() => await _localClients.DidNotReceive().RecordDeclaredProjections(Arg.Any<ConnectionId>(), Arg.Any<Cratis.Chronicle.Concepts.EventStoreName>(), Arg.Any<IEnumerable<ProjectionId>>());
}
