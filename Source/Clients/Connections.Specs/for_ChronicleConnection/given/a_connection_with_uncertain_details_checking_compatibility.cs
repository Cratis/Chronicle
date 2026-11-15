// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Contracts.Clients;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.given;

/// <summary>
/// A connection with compatibility checking enabled whose selected host and port are really user information.
/// </summary>
public class a_connection_with_uncertain_details_checking_compatibility : Specification
{
    protected const string Username = "synthetic-compat-user-marker";
    protected const string NumericPassword = "17531";
    protected const string Tail = "synthetic-compat-tail-marker";

    protected ChronicleConnection _connection;
    protected a_logger_that_remembers_messages<ChronicleConnection> _logger;
    protected IConnectionService _connectionService;
    protected Exception _failure;

    void Establish()
    {
        _logger = new a_logger_that_remembers_messages<ChronicleConnection>();
        _connectionService = Substitute.For<IConnectionService>();

        var connectionString = new ChronicleConnectionString($"chronicle://{Username}:{NumericPassword}?apiKey={Tail}@server:35000");
        var lifecycle = Substitute.For<IConnectionLifecycle>();
        lifecycle.IsConnected.Returns(false);
        lifecycle.ConnectionId.Returns(ConnectionId.New());
        lifecycle.Connected().Returns(Task.CompletedTask);

        var resolver = Substitute.For<IChronicleServerAddressResolver>();
        resolver
            .Resolve(Arg.Any<ChronicleConnectionString>())
            .Returns(Task.FromResult<IReadOnlyList<ChronicleServerAddress>>([new ChronicleServerAddress(Username, int.Parse(NumericPassword))]));

        _connection = new ChronicleConnection(
            connectionString,
            2,
            null,
            null,
            lifecycle,
            new Cratis.Tasks.TaskFactory(),
            Substitute.For<ICorrelationIdAccessor>(),
            NullLoggerFactory.Instance,
            CancellationToken.None,
            _logger,
            skipTlsValidation: true,
            skipCompatibilityCheck: false,
            skipKeepAlive: true,
            serverAddressResolver: resolver);
    }

    /// <summary>
    /// Runs the compatibility check against the substituted service, without dialing a real server.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    protected async Task CheckCompatibility()
    {
        // The service is constructed internally by Connect; isolate the check with a selected address and a substitute.
        typeof(ChronicleConnection).GetField("_currentServerAddress", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(_connection, new ChronicleServerAddress(Username, int.Parse(NumericPassword)));
        var check = typeof(ChronicleConnection).GetMethod("CheckCompatibility", BindingFlags.Instance | BindingFlags.NonPublic)!;
        _failure = await Catch.Exception(() => (Task)check.Invoke(_connection, [_connectionService])!);
    }

    protected void AssertNothingSensitiveWasExposed()
    {
        string[] markers = [Username, NumericPassword, Tail];
        diagnostic_payloads.ShouldNotExpose(_logger.Messages, markers);
        diagnostic_payloads.ShouldNotExpose(_logger.States.SelectMany(state => state).SelectMany(argument => diagnostic_payloads.Text(argument.Value)), markers);
        diagnostic_payloads.ShouldNotExpose(_logger.Exceptions.SelectMany(diagnostic_payloads.Text), markers);
        if (_failure is not null)
        {
            diagnostic_payloads.ShouldNotExpose(diagnostic_payloads.Text(_failure).Append(_failure.ToString()), markers);
        }
    }

    void Destroy() => _connection.Dispose();
}
