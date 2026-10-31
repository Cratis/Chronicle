// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ClientConnection;

public class when_unsubscribing_on_a_fresh_activation : given.a_client_connection
{
    Task Because() => _connection.Unsubscribe(_first);

    [Fact] void should_not_be_kept_alive() => _connection.PinCount.ShouldEqual(0);
    [Fact] void should_request_deactivation() => _connection.ReleaseCount.ShouldEqual(1);
}
