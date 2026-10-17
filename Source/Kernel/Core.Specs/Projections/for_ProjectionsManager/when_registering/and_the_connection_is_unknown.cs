// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

public class and_the_connection_is_unknown : given.a_manager_with_an_orphaned_projection
{
    async Task Because() => await _grain.Register([_definition], connectionId: ConnectionId.New());

    [Fact] async Task should_not_invent_a_local_declaration() => await _localClients.DidNotReceive().RecordDeclaredProjections(Arg.Any<ConnectionId>(), Arg.Any<EventStoreName>(), Arg.Any<IEnumerable<ProjectionId>>());
    [Fact] async Task should_not_invent_a_remote_declaration() => await _remoteClients.DidNotReceive().RecordDeclaredProjections(Arg.Any<ConnectionId>(), Arg.Any<EventStoreName>(), Arg.Any<IEnumerable<ProjectionId>>());
}
