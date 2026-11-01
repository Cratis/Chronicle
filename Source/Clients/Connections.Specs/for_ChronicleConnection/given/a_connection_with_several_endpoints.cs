// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
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

    protected virtual IReadOnlyList<ChronicleServerAddress> Servers => [_a, _b];
    protected virtual bool WatchdogTicksQuickly => false;
    protected virtual bool SkipCompatibilityCheck => true;

    void Establish()
    {
        _time = new ManualTimeProvider();
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
            return Task.CompletedTask;
        });
        _lifecycle.Disconnected().Returns(_ =>
        {
            _isConnected = false;
            _disconnectedCalls++;
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
            .Returns(call => Task.Delay(WatchdogTicksQuickly ? 1 : Timeout.Infinite, call.Arg<CancellationToken>()));
    }

    void CreateConnection(IChronicleServerAddressResolver resolver)
    {
        _connection = new ChronicleConnection(
            ChronicleConnectionString.Default,
            ConnectTimeoutSeconds,
            null,
            null,
            _lifecycle,
            _tasks,
            Substitute.For<ICorrelationIdAccessor>(),
            NullLoggerFactory.Instance,
            _cancellation.Token,
            NullLogger<ChronicleConnection>.Instance,
            skipTlsValidation: true,
            skipCompatibilityCheck: SkipCompatibilityCheck,
            skipKeepAlive: false,
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
    /// Waits for something to happen on another thread, failing the spec rather than hanging it.
    /// </summary>
    /// <param name="condition">The condition to wait for.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="TimeoutException">Thrown when the condition is not met in time.</exception>
    protected static async Task WaitUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("The condition was never met");
            }

            await Task.Delay(10);
        }
    }

    protected static RpcException Unavailable() => new(new Status(StatusCode.Unavailable, "node is restarting"));
}
