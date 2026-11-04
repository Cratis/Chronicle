// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_connecting;

public class directly_without_a_transport : given.a_gated_connection_service
{
    async Task Because()
    {
        var stream = Start(new CancellationTokenSource());
        await stream.Connected.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_use_the_internal_owner() => _ownership.GetClaim("connection")!.Owner.ShouldEqual(ConnectionOwner.InProcess);
}
