// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilderExtensions;

public class when_chaining_multiple_extensions : Specification
{
    ChronicleConnectionStringBuilder _builder;
    string _result;

    void Establish() => _builder = new ChronicleConnectionStringBuilder();

    void Because() => _result = ChronicleConnectionStringBuilderExtensions.Build(_builder
        .WithHost("example.com")
        .WithPort(8080)
        .WithCredentials("user", "pass")
        .WithTlsDisabled());

    [Fact] void should_build_complete_url() => _result.ShouldEqual("chronicle://user:pass@example.com:8080?disableTls=true");
}
