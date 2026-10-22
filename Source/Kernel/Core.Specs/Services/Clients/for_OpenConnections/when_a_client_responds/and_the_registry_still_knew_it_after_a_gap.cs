// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Clients.for_OpenConnections.when_a_client_responds;

/// <summary>
/// A gap only matters if it may have got the client dropped; the registry still knowing it afterwards says it did not.
/// </summary>
public class and_the_registry_still_knew_it_after_a_gap : given.an_open_connection
{
    bool _restored;

    void Because()
    {
        Responded(registryKnowsClient: true, TimeSpan.FromSeconds(7));
        _restored = Responded(registryKnowsClient: false, TimeSpan.FromSeconds(8));
    }

    [Fact] void should_restore_it_when_the_registry_is_lost_later() => _restored.ShouldBeTrue();
}
