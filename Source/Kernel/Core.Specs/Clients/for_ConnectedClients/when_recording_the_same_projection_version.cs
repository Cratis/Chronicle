// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectedClients;

public class when_recording_the_same_projection_version : given.a_versioned_projection_registration
{
    async Task Because() => await _clients.RecordDeclaredProjectionVersions(_connection, _eventStore, [_projection], "1.2.0");

    [Fact] async Task should_preserve_the_grace_period_start() => (await _clients.GetProjectionRegistrations(_eventStore, _projection)).Single().RegisteredAt.ShouldEqual(_registration.RegisteredAt);
}
