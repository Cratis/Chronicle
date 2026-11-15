// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionString;

public class when_parsing_url_with_a_numeric_password_and_an_unencoded_slash : Specification
{
    ChronicleConnectionString _parsed;
    Exception _result;

    void Because() => _result = Catch.Exception(() => _parsed = new ChronicleConnectionString("chronicle://user:123/x@host:35000"));

    [Fact] void should_fail_with_invalid_user_info() => _result.ShouldBeOfExactType<InvalidUserInfo>();
}
