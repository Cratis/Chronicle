// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilder;

/// <summary>
/// A connection string carries arbitrary options, so redaction falls back to the option name to catch a
/// credential the builder does not model - here the <c>key</c> in <c>auth=ApiKey&amp;key=...</c>.
/// </summary>
public class when_building_redacted_connection_string_with_an_unmodelled_credential_option : Specification
{
    ChronicleConnectionStringBuilder _builder;
    string _url;

    void Establish() => _builder = new ChronicleConnectionStringBuilder("chronicle://localhost?auth=ApiKey&key=testkey");

    void Because() => _url = _builder.BuildRedacted();

    [Fact] void should_keep_the_non_sensitive_option() => _url.Contains("auth=ApiKey", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_mask_the_sensitive_option() => _url.Contains("key=***", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_expose_the_sensitive_option() => _url.Contains("testkey", StringComparison.Ordinal).ShouldBeFalse();
}
