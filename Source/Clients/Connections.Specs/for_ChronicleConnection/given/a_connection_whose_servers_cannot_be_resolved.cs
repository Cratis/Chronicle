// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.given;

/// <summary>
/// A keep-alive connection to a <c language="text">chronicle+srv</c> address whose servers fail to resolve with the spec's failure.
/// </summary>
public abstract class a_connection_whose_servers_cannot_be_resolved : Specification
{
    protected IConnectionLifecycle _lifecycle;
    protected IChronicleServerAddressResolver _resolver;
    protected a_logger_that_remembers_messages<ChronicleConnection> _logger;
    protected ChronicleConnection _connection;
    protected int _resolveAttempts;

    protected abstract Exception ResolutionFailure { get; }

    void Establish()
    {
        _logger = new a_logger_that_remembers_messages<ChronicleConnection>();
        _lifecycle = Substitute.For<IConnectionLifecycle>();
        _lifecycle.IsConnected.Returns(false);
        _lifecycle.ConnectionId.Returns(ConnectionId.New());
        _lifecycle.Connected().Returns(Task.CompletedTask);

        _resolver = Substitute.For<IChronicleServerAddressResolver>();
        _resolver
            .Resolve(Arg.Any<ChronicleConnectionString>())
            .Returns(_ =>
            {
                Interlocked.Increment(ref _resolveAttempts);
                return Task.FromException<IReadOnlyList<ChronicleServerAddress>>(ResolutionFailure);
            });

        _connection = new ChronicleConnection(
            new ChronicleConnectionString("chronicle+srv://chronicle.example.local"),
            2,
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
            skipKeepAlive: false,
            serverAddressResolver: _resolver);
    }

    void Destroy() => _connection.Dispose();
}
