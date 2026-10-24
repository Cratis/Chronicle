// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Generations.Deployment;

namespace Cratis.Chronicle.Clients.for_ConnectedClients.given;

public class a_versioned_projection_registration : a_connection_with_projection_declarations
{
    protected ProjectionRegistration _registration;

    async Task Establish()
    {
        await _clients.RecordDeclaredProjectionVersions(_connection, _eventStore, [_projection], "1.2.0");
        _registration = (await _clients.GetProjectionRegistrations(_eventStore, _projection)).Single();
    }
}
