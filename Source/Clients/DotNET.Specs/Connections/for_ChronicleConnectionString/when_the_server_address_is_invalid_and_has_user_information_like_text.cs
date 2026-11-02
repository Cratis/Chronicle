// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionString;

public class when_the_server_address_is_invalid_and_has_user_information_like_text : Specification
{
    ChronicleConnectionString _parsed;
    Exception _result;

    void Because() => _result = Catch.Exception(() => _parsed = new ChronicleConnectionString("chronicle://alice:pa"));

    [Fact] void should_reject_the_address() => _result.ShouldBeOfExactType<InvalidServerAddress>();
    [Fact] void should_not_expose_the_username() => _result.Message.Contains("alice", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_expose_the_text_before_the_port() => _result.Message.Contains("'pa", StringComparison.Ordinal).ShouldBeFalse();
}
