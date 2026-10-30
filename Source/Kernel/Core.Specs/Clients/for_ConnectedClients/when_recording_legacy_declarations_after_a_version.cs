// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectedClients;

public class when_recording_legacy_declarations_after_a_version : given.a_versioned_projection_registration
{
    async Task Because() => await _clients.RecordDeclaredProjections(_connection, _eventStore, [_projection]);

    [Fact] async Task should_preserve_the_registered_version() => (await _clients.GetProjectionRegistrations(_eventStore, _projection)).Single().ShouldEqual(_registration);
}
