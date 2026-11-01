// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_the_keep_alive_stream;

public class and_a_stale_subscription_reports_an_error : given.a_connection_with_several_endpoints
{
    protected override bool WatchdogTicksQuickly => true;
    protected override IReadOnlyList<ChronicleServerAddress> Servers => [_a];

    async Task Because()
    {
        var first = new UndisposableStream();
        var streams = 0;
        _streams[_a] = () => ++streams == 1 ? first : Healthy();
        await StartConnecting().WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        first.Observer!.OnCompleted();
        await WaitUntil(() => _servedBy.Count == 2 && _isConnected);

        // The replaced attempt reports a failure after the connection has moved on.
        first.Observer!.OnError(Unavailable());
        await Task.Delay(200);
    }

    [Fact] void should_still_be_connected() => _isConnected.ShouldBeTrue();
    [Fact] void should_not_reconnect_again() => _servedBy.Count.ShouldEqual(2);
    [Fact] void should_report_the_disconnect_once() => _disconnectedCalls.ShouldEqual(1);

    class UndisposableStream : IObservable<ConnectionKeepAlive>
    {
        public IObserver<ConnectionKeepAlive>? Observer { get; private set; }

        public IDisposable Subscribe(IObserver<ConnectionKeepAlive> observer)
        {
            Observer = observer;
            observer.OnNext(new ConnectionKeepAlive());
            return System.Reactive.Disposables.Disposable.Empty;
        }
    }
}
