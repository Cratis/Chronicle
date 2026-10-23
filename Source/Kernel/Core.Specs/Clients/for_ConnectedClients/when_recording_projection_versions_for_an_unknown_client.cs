// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Clients.for_ConnectedClients;

public class when_recording_projection_versions_for_an_unknown_client : given.a_connection_with_projection_declarations
{
    async Task Because() => await _clients.RecordDeclaredProjectionVersions(ConnectionId.New(), _eventStore, ["another-projection"], "1.2.0");

    [Fact] async Task should_not_invent_a_registration() => (await _clients.GetProjectionRegistrations(_eventStore, "another-projection")).ShouldBeEmpty();
}
