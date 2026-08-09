// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionString;

/// <summary>
/// Redaction is only ever reached from a diagnostics path, so it must never be the thing that
/// brings a connection down - not even for userinfo that does not parse cleanly.
/// </summary>
public class when_redacting_a_malformed_connection_string : Specification
{
    const string Secret = "p@ss:word";

    ChronicleConnectionString _connectionString;
    string _result;
    Exception _exception;

    void Establish() => _connectionString = new ChronicleConnectionString($"chronicle://cratis-studio:{Secret}@localhost:35000");

    void Because() => _exception = Catch.Exception(() => _result = _connectionString.Redacted);

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_mask_the_userinfo() => _result.ShouldEqual("chronicle://cratis-studio:***@localhost:35000");
    [Fact] void should_not_expose_any_part_of_the_password() => _result.Contains("ss", StringComparison.Ordinal).ShouldBeFalse();
}
