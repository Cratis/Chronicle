// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Clients.for_OpenConnections.when_a_client_responds;

public class and_its_connection_was_closed : given.an_open_connection
{
    bool _restored;

    void Because()
    {
        Close();
        _restored = Responded(registryKnowsClient: false, TimeSpan.FromSeconds(1));
    }

    [Fact] void should_not_restore_it() => _restored.ShouldBeFalse();
}
