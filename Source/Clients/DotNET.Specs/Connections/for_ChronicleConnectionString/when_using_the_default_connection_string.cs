// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionString;

public class when_using_the_default_connection_string : Specification
{
    ChronicleConnectionString _connectionString;

    void Because() => _connectionString = ChronicleConnectionString.Default;

    [Fact] void should_validate_tls() => _connectionString.SkipTlsValidation.ShouldBeFalse();
    [Fact] void should_not_carry_a_validation_bypass() => _connectionString.ToString().ShouldNotContain("skipTlsValidation");
}
