// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectedClients;

public class when_reconnecting_a_versioned_client : given.a_versioned_projection_registration
{
    async Task Because() => await Connect(_connection);

    [Fact] async Task should_not_retain_the_old_registration() => (await _clients.GetProjectionRegistrations(_eventStore, _projection)).ShouldBeEmpty();
}
