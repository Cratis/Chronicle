// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.given;

public class a_connection_that_cannot_reach_the_kernel : Specification
{
    protected const int ConnectTimeoutSeconds = 2;

    protected ChronicleConnection _connection;
    protected IConnectionLifecycle _lifecycle;
    protected IChronicleServerAddressResolver _serverAddressResolver;
    protected ControllableTimeProvider _time;
    protected int _resolveAttempts;

    void Establish()
    {
        _time = new ControllableTimeProvider();
        _lifecycle = Substitute.For<IConnectionLifecycle>();
        _lifecycle.IsConnected.Returns(false);
        _lifecycle.ConnectionId.Returns(ConnectionId.New());

        // Failing to resolve an address is how an unreachable kernel presents before a channel is even dialed,
        // and it makes the attempt fail without dialing one from a spec.
        _serverAddressResolver = Substitute.For<IChronicleServerAddressResolver>();
        _serverAddressResolver
            .Resolve(Arg.Any<ChronicleConnectionString>())
            .Returns(_ =>
            {
                _resolveAttempts++;
                return Task.FromException<IReadOnlyList<ChronicleServerAddress>>(new UnableToResolveClientUri());
            });

        _connection = new ChronicleConnection(
            ChronicleConnectionString.Default,
            ConnectTimeoutSeconds,
            null,
            null,
            _lifecycle,
            new Cratis.Tasks.TaskFactory(),
            Substitute.For<ICorrelationIdAccessor>(),
            NullLoggerFactory.Instance,
            CancellationToken.None,
            NullLogger<ChronicleConnection>.Instance,
            skipTlsValidation: true,
            skipCompatibilityCheck: true,
            skipKeepAlive: true,
            serverAddressResolver: _serverAddressResolver,
            timeProvider: _time);
    }

    void Destroy() => _connection.Dispose();
}
