// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilder;

/// <summary>
/// A connection string carrying unmodelled options has no proven diagnostic projection.
/// </summary>
public class when_building_redacted_connection_string_with_an_unmodelled_credential_option : Specification
{
    ChronicleConnectionStringBuilder _builder;
    string _url;

    void Establish() => _builder = new ChronicleConnectionStringBuilder("chronicle://localhost?auth=ApiKey&key=testkey");

    void Because() => _url = _builder.BuildRedacted();

    [Fact] void should_not_echo_unmodelled_options() => _url.Contains("auth=ApiKey", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_use_opaque_diagnostics() => _url.ShouldEqual("REDACTED");
    [Fact] void should_not_expose_the_sensitive_option() => _url.Contains("testkey", StringComparison.Ordinal).ShouldBeFalse();
}
