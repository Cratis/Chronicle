// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.Services.Clients.for_OpenConnections.when_a_client_responds;

public class and_the_registry_lost_it_while_it_kept_responding : given.an_open_connection
{
    bool _restore;
    ConnectRequest? _restoreFrom;

    void Because()
    {
        Responded(registryKnowsClient: true, TimeSpan.FromSeconds(1));
        Responded(registryKnowsClient: true, TimeSpan.FromSeconds(2));
        _restore = Responded(registryKnowsClient: false, TimeSpan.FromSeconds(3), out _restoreFrom);
    }

    [Fact] void should_restore_the_client() => _restore.ShouldBeTrue();
    [Fact] void should_restore_it_from_the_request_it_connected_with() => _restoreFrom.ShouldEqual(_request);
}
