// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ConnectionUnavailable;

public class when_creating_with_connection_string_text : Specification
{
    ConnectionUnavailable _result;

    void Because() => _result = new ConnectionUnavailable("chronicle://user:s3cret@host:35000");

    [Fact] void should_not_expose_the_password() => _result.Message.Contains("s3cret", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_expose_the_username() => _result.Message.Contains("//user", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_mention_the_server() => _result.Message.Contains("host:35000", StringComparison.Ordinal).ShouldBeTrue();
}
