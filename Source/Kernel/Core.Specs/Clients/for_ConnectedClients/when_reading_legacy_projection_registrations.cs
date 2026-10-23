// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectedClients;

public class when_reading_legacy_projection_registrations : given.a_connection_with_projection_declarations
{
    Cratis.Chronicle.Projections.Generations.Deployment.ProjectionRegistration _registration;

    async Task Because() => _registration = (await _clients.GetProjectionRegistrations(_eventStore, _projection)).Single();

    [Fact] void should_keep_the_declaring_connection() => _registration.ConnectionId.ShouldEqual(_connection);
    [Fact] void should_report_the_missing_version() => _registration.RegisteredVersion.ShouldBeNull();
}
