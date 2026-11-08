// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionString;

public class when_redacting_a_connection_string_with_an_ipv6_host : Specification
{
    string _result;

    void Because() => _result = new ChronicleConnectionString("chronicle://user:s3cret@[::1]:35000").Redacted;

    [Fact] void should_not_expose_the_password() => _result.Contains("s3cret", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_expose_the_username() => _result.Contains("//user", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_keep_the_host() => _result.Contains("::1", StringComparison.Ordinal).ShouldBeTrue();
}
