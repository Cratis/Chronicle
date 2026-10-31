// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectedClients.when_reconnecting;

public class and_the_same_connection_id_is_reused : given.a_connection_with_projection_declarations
{
    async Task Because() => await Connect(_connection);

    [Fact] async Task should_require_new_declarations() => (await _clients.DeclaresProjection(_eventStore, _projection)).ShouldBeFalse();
}
