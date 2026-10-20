// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Clients.for_OpenConnections.when_a_client_responds;

/// <summary>
/// While a silo leaves the cluster the registry call handling a response can be held up for longer than the stale
/// threshold. The client waits for it before it can respond again, so that wait is not the client going quiet.
/// </summary>
public class and_handling_its_previous_response_was_held_up : given.an_open_connection
{
    bool _restored;

    void Because()
    {
        Responded(registryKnowsClient: true, TimeSpan.FromSeconds(1), handledAfter: TimeSpan.FromSeconds(9), out _);
        _restored = Responded(registryKnowsClient: false, TimeSpan.FromSeconds(10));
    }

    [Fact] void should_restore_it() => _restored.ShouldBeTrue();
}
