// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectedClients;

public class when_recording_projection_versions : given.a_connection_with_projection_declarations
{
    DateTimeOffset _before;

    async Task Because()
    {
        _before = DateTimeOffset.UtcNow;
        await _clients.RecordDeclaredProjectionVersions(_connection, _eventStore, [_projection, "another-projection"], "1.2.0");
    }

    [Fact] async Task should_read_back_the_registered_version() => (await _clients.GetProjectionRegistrations(_eventStore, _projection)).Single().RegisteredVersion.ShouldEqual("1.2.0");
    [Fact] async Task should_identify_the_connection() => (await _clients.GetProjectionRegistrations(_eventStore, _projection)).Single().ConnectionId.ShouldEqual(_connection);
    [Fact] async Task should_record_when_the_version_was_seen() => ((await _clients.GetProjectionRegistrations(_eventStore, _projection)).Single().RegisteredAt >= _before).ShouldBeTrue();
    [Fact] async Task should_record_every_projection() => (await _clients.GetProjectionRegistrations(_eventStore, "another-projection")).Single().RegisteredVersion.ShouldEqual("1.2.0");
    [Fact] async Task should_not_leak_across_stores() => (await _clients.GetProjectionRegistrations("another-store", _projection)).ShouldBeEmpty();
}
