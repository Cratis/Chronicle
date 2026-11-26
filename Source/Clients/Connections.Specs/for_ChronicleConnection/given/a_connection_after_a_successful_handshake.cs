// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.given;

public class a_connection_after_a_successful_handshake : a_connection_with_several_endpoints
{
    protected override bool SkipCompatibilityCheck => false;
    protected override bool WatchdogTicksQuickly => true;
    protected override IReadOnlyList<ChronicleServerAddress> Servers => [_a];

    async Task Establish()
    {
        _streams[_a] = Healthy;
        await StartConnecting().WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        _time.Advance(TimeSpan.FromSeconds(6));
    }
}
