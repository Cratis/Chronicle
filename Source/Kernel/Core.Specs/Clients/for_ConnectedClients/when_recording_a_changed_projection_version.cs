// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectedClients;

public class when_recording_a_changed_projection_version : given.a_versioned_projection_registration
{
    async Task Because() => await _clients.RecordDeclaredProjectionVersions(_connection, _eventStore, [_projection], "1.3.0");

    [Fact] async Task should_replace_the_registered_version() => (await _clients.GetProjectionRegistrations(_eventStore, _projection)).Single().RegisteredVersion.ShouldEqual("1.3.0");
    [Fact] async Task should_restart_the_grace_period() => ((await _clients.GetProjectionRegistrations(_eventStore, _projection)).Single().RegisteredAt >= _registration.RegisteredAt).ShouldBeTrue();
}
