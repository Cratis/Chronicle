// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting;

public class and_client_certificate_diagnostics_are_logged : Specification
{
    const string Username = "synthetic-channel-user-marker";
    const string Password = "synthetic-logger-certificate-password-marker";
    const string CertificateMarker = "synthetic-logger-certificate-path-marker";
    string _certificatePath;
    ChronicleConnection _connection;
    given.a_logger_that_remembers_messages<ChronicleConnection> _logger;

    void Establish()
    {
        _certificatePath = Path.Combine(Path.GetTempPath(), $"{CertificateMarker}-{Guid.NewGuid():N}.pfx");
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=chronicle-diagnostic-specs", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        File.WriteAllBytes(_certificatePath, certificate.Export(X509ContentType.Pkcs12, Password));

        _logger = new();
        var connectionString = new ChronicleConnectionString($"chronicle://{Username}:52139?apiKey=synthetic-channel-tail-marker@server:35000");
        var lifecycle = Substitute.For<IConnectionLifecycle>();
        lifecycle.ConnectionId.Returns(ConnectionId.New());
        lifecycle.Connected().Returns(Task.CompletedTask);
        var resolver = Substitute.For<IChronicleServerAddressResolver>();
        resolver.Resolve(Arg.Any<ChronicleConnectionString>()).Returns(Task.FromResult<IReadOnlyList<ChronicleServerAddress>>([connectionString.ServerAddress]));
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
            certificatePath: _certificatePath,
            certificatePassword: Password,
            skipCompatibilityCheck: true,
            skipKeepAlive: true,
            serverAddressResolver: resolver);
    }

    async Task Because() => await _connection.Connect();

    void Destroy()
    {
        _connection?.Dispose();
        if (File.Exists(_certificatePath))
        {
            File.Delete(_certificatePath);
        }
    }

    [Fact] void should_reach_the_client_certificate_diagnostic() => _logger.States.SelectMany(state => state).Any(argument => argument.Key == "Path").ShouldBeTrue();
    [Fact] void should_reach_the_channel_diagnostic() => _logger.States.SelectMany(state => state).Any(argument => argument.Key == "Address").ShouldBeTrue();
    [Fact] void should_not_expose_markers_in_formatted_logs() => AssertSafe(_logger.Messages);
    [Fact] void should_not_expose_markers_in_structured_values() => AssertSafe(_logger.States.SelectMany(state => state).SelectMany(argument => given.diagnostic_payloads.Text(argument.Value)));
    [Fact] void should_not_expose_markers_in_logged_exception_objects() => AssertSafe(_logger.Exceptions.SelectMany(given.diagnostic_payloads.Text));

    void AssertSafe(IEnumerable<string> values) => given.diagnostic_payloads.ShouldNotExpose(values, _certificatePath, CertificateMarker, Password, Username, "52139", "synthetic-channel-tail-marker");
}
