// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections.for_ConnectionStringDiagnostics.given;

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionString;

public class when_preserving_rejected_input_compatibility : Specification
{
    Exception _unknownScheme;
    Exception _missingServer;
    Exception _invalidPort;
    Exception _invalidUserInfo;

    void Because()
    {
        _unknownScheme = Catch.Exception(() => _ = new ChronicleConnectionString("synthetic://server:35000"));
        _missingServer = Catch.Exception(() => _ = new ChronicleConnectionString("chronicle://"));
        _invalidPort = Catch.Exception(() => _ = new ChronicleConnectionString("chronicle://server:synthetic-port-marker"));
        _invalidUserInfo = Catch.Exception(() => _ = new ChronicleConnectionString($"chronicle://{a_connection_string_corpus.Username}:nonnumeric?neutral={a_connection_string_corpus.Tail}"));
    }

    [Fact] void should_reject_unknown_schemes() => _unknownScheme.ShouldBeOfExactType<ArgumentException>();
    [Fact] void should_reject_missing_servers() => _missingServer.ShouldBeOfExactType<MissingServerAddress>();
    [Fact] void should_reject_invalid_ports() => _invalidPort.ShouldBeOfExactType<InvalidServerAddress>();
    [Fact] void should_reject_misplaced_userinfo() => _invalidUserInfo.ShouldBeOfExactType<InvalidUserInfo>();
}
