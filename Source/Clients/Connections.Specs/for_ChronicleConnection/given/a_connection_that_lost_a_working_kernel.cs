// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.given;

/// <summary>
/// A connection that established itself once and then lost the kernel - the case #3948 reports. The failure state
/// deliberately does not arm before the first successful connect, so a spec about it has to connect first.
/// </summary>
public class a_connection_that_lost_a_working_kernel : Specification
{
    protected const int ConnectTimeoutSeconds = 2;

    protected ChronicleConnection _connection;
    protected a_logger_that_remembers_messages<ChronicleConnection> _logger;
    protected IConnectionLifecycle _lifecycle;
    protected IChronicleServerAddressResolver _serverAddressResolver;
    protected ControllableTimeProvider _time;
    protected bool _isConnected;
    protected bool _kernelIsReachable;
    protected int _resolveAttempts;

    protected virtual ChronicleConnectionString ConnectionString => ChronicleConnectionString.Default;

    void Establish()
    {
        _logger = new a_logger_that_remembers_messages<ChronicleConnection>();
        _time = new ControllableTimeProvider();
        _kernelIsReachable = true;
        _isConnected = false;

        _lifecycle = Substitute.For<IConnectionLifecycle>();
        _lifecycle.IsConnected.Returns(_ => _isConnected);
        _lifecycle.ConnectionId.Returns(ConnectionId.New());
        _lifecycle.Connected().Returns(_ =>
        {
            _isConnected = true;
            return Task.CompletedTask;
        });

        // Failing to resolve an address is how an unreachable kernel presents before a channel is even dialed,
        // and it makes an attempt fail without dialing one from a spec.
        _serverAddressResolver = Substitute.For<IChronicleServerAddressResolver>();
        _serverAddressResolver
            .Resolve(Arg.Any<ChronicleConnectionString>())
            .Returns(_ =>
            {
                _resolveAttempts++;
                return _kernelIsReachable
                    ? Task.FromResult<IReadOnlyList<ChronicleServerAddress>>([ChronicleConnectionString.Default.ServerAddress])
                    : Task.FromException<IReadOnlyList<ChronicleServerAddress>>(new UnableToResolveClientUri());
            });

        _connection = new ChronicleConnection(
            ConnectionString,
            ConnectTimeoutSeconds,
            null,
            null,
            _lifecycle,
            new Cratis.Tasks.TaskFactory(),
            Substitute.For<ICorrelationIdAccessor>(),
            NullLoggerFactory.Instance,
            CancellationToken.None,
            _logger,
            skipTlsValidation: true,
            skipCompatibilityCheck: true,
            skipKeepAlive: true,
            serverAddressResolver: _serverAddressResolver,
            timeProvider: _time);
    }

    /// <summary>
    /// Connect once against a reachable kernel, then lose it - leaving the connection in the state the issue is about.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    protected async Task ConnectThenLoseTheKernel()
    {
        await _connection.Connect();
        _isConnected = false;
        _kernelIsReachable = false;
        _resolveAttempts = 0;
    }

    void Destroy() => _connection.Dispose();
}
