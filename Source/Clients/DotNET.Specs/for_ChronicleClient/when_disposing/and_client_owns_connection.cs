// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_ChronicleClient.when_disposing;

public class and_client_owns_connection : Specification
{
    ChronicleClient _client;
    CancellationToken _ownedConnectionCancellationToken;

    void Establish()
    {
        _client = new ChronicleClient(new ChronicleOptions
        {
            AutoDiscoverAndRegister = false
        });
        _ownedConnectionCancellationToken = _client.OwnedConnectionCancellationToken;
    }

    void Because() => _client.Dispose();

    [Fact] void should_cancel_connection_token() => _ownedConnectionCancellationToken.IsCancellationRequested.ShouldBeTrue();
}
