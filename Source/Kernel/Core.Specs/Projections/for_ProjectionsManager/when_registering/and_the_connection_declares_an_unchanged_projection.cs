// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

public class and_the_connection_declares_an_unchanged_projection : given.a_manager_with_an_orphaned_projection
{
    readonly ConnectionId _connection = ConnectionId.New();

    void Establish() => _localClients.IsConnected(_connection).Returns(true);
    async Task Because() => await _grain.Register([_definition], connectionId: _connection);

    [Fact] async Task should_record_the_declaration_even_without_a_definition_change() => await _localClients.Received(1).RecordDeclaredProjections(_connection, EventStore, Arg.Is<IEnumerable<Cratis.Chronicle.Concepts.Projections.ProjectionId>>(ids => ids.Single() == _definition.Identifier));
}
