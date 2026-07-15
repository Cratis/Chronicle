// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionString;

public class when_using_the_development_connection_string : Specification
{
    ChronicleConnectionString _connectionString;

    void Because() => _connectionString = ChronicleConnectionString.Development;

    [Fact] void should_skip_tls_validation() => _connectionString.SkipTlsValidation.ShouldBeTrue();
}
