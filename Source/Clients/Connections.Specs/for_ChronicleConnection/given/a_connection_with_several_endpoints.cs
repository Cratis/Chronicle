// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Threading.Channels;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.given;

/// <summary>
/// A connection over several servers whose keep-alive streams the spec controls per server. Time is manual, so
/// a spec decides when a handshake has run out of time.
/// </summary>
public class a_connection_with_several_endpoints : Specification
{
    protected const int ConnectTimeoutSeconds = 30;

    protected static readonly ChronicleServerAddress _a = new("node-a", 35000);
    protected static readonly ChronicleServerAddress _b = new("node-b", 35000);
    protected static readonly ChronicleServerAddress _c = new("node-c", 35000);

    protected ChronicleConnection _connection;
    protected a_logger_that_remembers_messages<ChronicleConnection> _logger;
    protected ManualTimeProvider _time;
    protected IConnectionLifecycle _lifecycle;
    protected ILoadBalancerStrategy _loadBalancer;
    protected ITaskFactory _tasks;
    protected CancellationTokenSource _cancellation;
    protected bool _isConnected;
    protected List<ChronicleServerAddress[]> _candidatesOffered;
    protected List<ChronicleServerAddress> _servedBy;
    protected Dictionary<ChronicleServerAddress, Func<IObservable<ConnectionKeepAlive>>> _streams;
    protected Dictionary<ChronicleServerAddress, int> _connectsPerServer;
    protected Dictionary<ChronicleServerAddress, Func<Task<CompatibilityResponse>>> _compatibility;
    protected int _disconnectedCalls;
    readonly Channel<TaskCompletionSource> _watchdogDelays = Channel.CreateUnbounded<TaskCompletionSource>();
    readonly object _changeGate = new();
    TaskCompletionSource _changed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    protected virtual ChronicleConnectionString ConnectionString => ChronicleConnectionString.Default;
    protected virtual IReadOnlyList<ChronicleServerAddress> Servers => [_a, _b];
    protected virtual bool WatchdogTicksQuickly => false;
    protected virtual bool SkipCompatibilityCheck => true;
    protected virtual bool SkipKeepAlive => false;

    void Establish()
    {
        _time = new ManualTimeProvider();
        _logger = new a_logger_that_remembers_messages<ChronicleConnection>();
        _cancellation = new CancellationTokenSource();
        _candidatesOffered = [];
        _servedBy = [];
        _connectsPerServer = [];
        _streams = [];
        _compatibility = [];
        foreach (var server in new[] { _a, _b, _c })
        {
            _streams[server] = Observable.Never<ConnectionKeepAlive>;
        }

        CreateLifecycle();
        var resolver = CreateResolver();
        CreateLoadBalancer();
        CreateTaskFactory();
        CreateConnection(resolver);
    }

    void CreateLifecycle()
    {
        _lifecycle = Substitute.For<IConnectionLifecycle>();
        _lifecycle.IsConnected.Returns(_ => _isConnected);
        _lifecycle.ConnectionId.Returns(ConnectionId.New());
        _lifecycle.Connected().Returns(_ =>
        {
            _isConnected = true;
            SignalChange();
            return Task.CompletedTask;
        });
        _lifecycle.Disconnected().Returns(_ =>
        {
            _isConnected = false;
            _disconnectedCalls++;
            SignalChange();
            return Task.CompletedTask;
        });
    }

    IChronicleServerAddressResolver CreateResolver()
    {
        var resolver = Substitute.For<IChronicleServerAddressResolver>();
        resolver.Resolve(Arg.Any<ChronicleConnectionString>())
            .Returns(_ => Task.FromResult<IReadOnlyList<ChronicleServerAddress>>(Servers));

        return resolver;
    }

    void CreateLoadBalancer()
    {
        // Picks the first candidate offered, so a spec sees the order the connection chose without the balancer's own preference.
        _loadBalancer = Substitute.For<ILoadBalancerStrategy>();
        _loadBalancer.Next(Arg.Any<IReadOnlyList<ChronicleServerAddress>>())
            .Returns(call =>
            {
                var offered = call.Arg<IReadOnlyList<ChronicleServerAddress>>().ToArray();
                _candidatesOffered.Add(offered);
                return Task.FromResult(offered[0]);
            });
    }

    void CreateTaskFactory()
    {
        // The watchdog judges staleness by the manual clock. Unless a spec is about it, its monitor never ticks, so it cannot interfere.
        _tasks = Substitute.For<ITaskFactory>();
        _tasks.Run(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.Run(call.Arg<Func<Task>>()));
        _tasks.Delay(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                // A delay never elapses by itself; it ends when the spec ticks the watchdog (or the connection is cancelled).
                var tick = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                call.Arg<CancellationToken>().Register(() => tick.TrySetCanceled());
                if (WatchdogTicksQuickly)
                {
                    _watchdogDelays.Writer.TryWrite(tick);
                }

                return tick.Task;
            });
    }

    void CreateConnection(IChronicleServerAddressResolver resolver)
    {
        _connection = new ChronicleConnection(
            ConnectionString,
            ConnectTimeoutSeconds,
            null,
            null,
            _lifecycle,
            _tasks,
            Substitute.For<ICorrelationIdAccessor>(),
            NullLoggerFactory.Instance,
            _cancellation.Token,
            _logger,
            skipTlsValidation: true,
            skipCompatibilityCheck: SkipCompatibilityCheck,
            skipKeepAlive: SkipKeepAlive,
            serverAddressResolver: resolver,
            loadBalancerStrategy: _loadBalancer,
            timeProvider: _time)
        {
            ConnectionServiceFactory = (address, _) =>
            {
                var service = Substitute.For<IConnectionService>();
                service.CheckCompatibility(Arg.Any<CompatibilityRequest>())
                    .Returns(_ => _compatibility.TryGetValue(address, out var check)
                        ? check()
                        : Task.FromResult(new CompatibilityResponse { IsCompatible = true }));
                service.Connect(Arg.Any<ConnectRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>())
                    .Returns(_ =>
                    {
                        _connectsPerServer[address] = _connectsPerServer.GetValueOrDefault(address) + 1;
                        _servedBy.Add(address);
                        SignalChange();
                        return _streams[address]();
                    });
                return service;
            }
        };
    }

    void Destroy()
    {
        _cancellation.Cancel();
        _connection.Dispose();
        _cancellation.Dispose();
    }

    /// <summary>
    /// Starts connecting.
    /// </summary>
    /// <returns>The connect task.</returns>
    protected Task StartConnecting() => _connection.Connect();

    protected string TriedInOrder => string.Join(',', _servedBy.Select(address => address.Host));

    protected string Offered(int round) => string.Join(',', _candidatesOffered[round].Select(address => address.Host));

    /// <summary>
    /// A keep-alive stream that delivers one keep-alive and then stays open.
    /// </summary>
    /// <returns>The stream.</returns>
    protected static IObservable<ConnectionKeepAlive> Healthy() =>
        Observable.Return(new ConnectionKeepAlive { ConnectionId = "connection" }).Concat(Observable.Never<ConnectionKeepAlive>());

    /// <summary>
    /// Ends the watchdog's current monitor delay and returns once the watchdog has acted on that tick and is waiting again.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    protected async Task TickWatchdog()
    {
        var tick = await _watchdogDelays.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        tick.TrySetResult();
        await _watchdogDelays.Reader.WaitToReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
    }

    /// <summary>
    /// Waits for something to happen on another thread, failing the spec rather than hanging it. The condition is
    /// re-evaluated whenever the connection reports progress.
    /// </summary>
    /// <param name="condition">The condition to wait for.</param>
    /// <returns>Awaitable task.</returns>
    protected async Task WaitUntil(Func<bool> condition)
    {
        while (true)
        {
            Task changed;
            lock (_changeGate)
            {
                changed = _changed.Task;
            }

            if (condition())
            {
                return;
            }

            await changed.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        }
    }

    void SignalChange()
    {
        lock (_changeGate)
        {
            _changed.TrySetResult();
            _changed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }

    protected static RpcException Unavailable() => new(new Status(StatusCode.Unavailable, "node is restarting"));
}
