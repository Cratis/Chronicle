// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Services.Projections.for_Projections.when_registering;

public class and_the_connection_is_local : given.a_registration_with_a_connection
{
    void Establish() => _localClients.IsConnected(_connection).Returns(true);
    async Task Because() => await _service.Register(_request);

    [Fact] async Task should_record_the_submitted_declarations() => await _localClients.Received(1).RecordDeclaredProjectionVersions(_connection, EventStore, Arg.Is<IEnumerable<ProjectionId>>(ids => ids.Single() == _projectionDefinition.Identifier), RegisteredVersion);
    [Fact] async Task should_not_query_other_silos() => await _management.DidNotReceive().GetHosts(true);
}
