// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_reconnecting;

public class and_every_endpoint_fails : given.a_connection_with_several_endpoints
{
    Exception? _result;

    async Task Because()
    {
        _streams[_a] = Healthy;
        await StartConnecting().WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        _isConnected = false;
        _servedBy.Clear();
        _streams[_a] = Observable.Never<ConnectionKeepAlive>;

        var connecting = StartConnecting();
        await _time.WhenNextTimer();
        _time.Advance(TimeSpan.FromSeconds(5));
        await _time.WhenNextTimer();
        _time.Advance(TimeSpan.FromSeconds(ConnectTimeoutSeconds - 5));
        try
        {
            await connecting.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        }
        catch (Exception ex)
        {
            _result = ex;
        }
    }

    [Fact] void should_time_out() => _result.ShouldBeOfExactType<ConnectionTimedOut>();
    [Fact] void should_try_each_endpoint_once() => TriedInOrder.ShouldEqual("node-a,node-b");
    [Fact] void should_not_be_connected() => _isConnected.ShouldBeFalse();
}
