// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Services.Projections.for_Projections.when_registering;

public class and_the_connection_is_unknown : given.a_registration_with_a_connection
{
    async Task Because() => await _service.Register(_request);

    [Fact] async Task should_not_invent_a_local_declaration() => await _localClients.DidNotReceive().RecordDeclaredProjections(Arg.Any<ConnectionId>(), Arg.Any<EventStoreName>(), Arg.Any<IEnumerable<ProjectionId>>());
    [Fact] async Task should_not_invent_a_remote_declaration() => await _remoteClients.DidNotReceive().RecordDeclaredProjections(Arg.Any<ConnectionId>(), Arg.Any<EventStoreName>(), Arg.Any<IEnumerable<ProjectionId>>());
}
