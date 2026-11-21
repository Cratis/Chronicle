// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_creating;

public class with_client_credentials_and_tls_enabled : given.a_connection_factory
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => _connection = CreateConnection(ChronicleConnectionString.Development));

    [Fact] void should_allow_credentials_over_tls() => _error.ShouldBeNull();
}
