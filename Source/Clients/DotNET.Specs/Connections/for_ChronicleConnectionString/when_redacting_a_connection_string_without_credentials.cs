// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionString;

public class when_redacting_a_connection_string_without_credentials : Specification
{
    ChronicleConnectionString _connectionString;
    string _result;

    void Establish() => _connectionString = new ChronicleConnectionString("chronicle://localhost:35000?loadBalancer=RoundRobin");

    void Because() => _result = _connectionString.Redacted;

    [Fact] void should_leave_the_connection_string_unchanged() => _connectionString.ToString().ShouldEqual("chronicle://localhost:35000?loadBalancer=RoundRobin");
    [Fact] void should_use_opaque_diagnostics_for_query_settings() => _result.ShouldEqual("REDACTED");
}
