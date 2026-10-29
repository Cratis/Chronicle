// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_creating;

public class with_client_credentials_and_tls_disabled : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() =>
    {
        using var connection = new ChronicleConnection(
            new ChronicleConnectionString("chronicle://someone:secret@localhost:35001/?disableTls=true"),
            1,
            null,
            null,
            Substitute.For<IConnectionLifecycle>(),
            new Cratis.Tasks.TaskFactory(),
            Substitute.For<ICorrelationIdAccessor>(),
            NullLoggerFactory.Instance,
            CancellationToken.None,
            NullLogger<ChronicleConnection>.Instance,
            skipTlsValidation: true,
            skipCompatibilityCheck: true,
            skipKeepAlive: true);
    });

    [Fact] void should_refuse_to_exchange_credentials_without_tls() => _error.ShouldBeOfExactType<ClientCredentialsRequireTls>();
}
