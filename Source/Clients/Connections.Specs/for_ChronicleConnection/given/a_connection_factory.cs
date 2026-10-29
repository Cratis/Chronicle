// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.given;

/// <summary>
/// Creates real connections over a substitute lifecycle and address resolver, with keep-alive not skipped by the constructor.
/// </summary>
public class a_connection_factory : Specification
{
    protected IConnectionLifecycle _lifecycle;
    protected ChronicleConnection _connection;

    void Establish()
    {
        _lifecycle = Substitute.For<IConnectionLifecycle>();
        _lifecycle.IsConnected.Returns(false);
        _lifecycle.ConnectionId.Returns(ConnectionId.New());
        _lifecycle.Connected().Returns(Task.CompletedTask);
    }

    protected ChronicleConnection CreateConnection(ChronicleConnectionString connectionString)
    {
        var resolver = Substitute.For<IChronicleServerAddressResolver>();
        resolver
            .Resolve(Arg.Any<ChronicleConnectionString>())
            .Returns(Task.FromResult<IReadOnlyList<ChronicleServerAddress>>([connectionString.ServerAddress]));

        return new ChronicleConnection(
            connectionString,
            2,
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
            skipKeepAlive: false,
            serverAddressResolver: resolver);
    }

    void Destroy() => _connection?.Dispose();
}
