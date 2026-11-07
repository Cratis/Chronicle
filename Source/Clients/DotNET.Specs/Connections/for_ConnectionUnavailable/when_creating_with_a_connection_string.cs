// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ConnectionUnavailable;

public class when_creating_with_a_connection_string : Specification
{
    ConnectionUnavailable _result;

    void Because() => _result = new ConnectionUnavailable(new ChronicleConnectionString("chronicle://user:s3cret@host:35000/?apiKey=s3cret"));

    [Fact] void should_not_expose_the_secrets() => _result.Message.Contains("s3cret", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_expose_the_username() => _result.Message.Contains("//user", StringComparison.Ordinal).ShouldBeFalse();
}
