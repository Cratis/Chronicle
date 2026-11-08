// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionString;

public class when_parsing_url_with_an_unencoded_question_mark_in_the_password : Specification
{
    ChronicleConnectionString _parsed;
    Exception _result;

    void Because() => _result = Catch.Exception(() => _parsed = new ChronicleConnectionString("chronicle://user:s3?cret@host:35000"));

    [Fact] void should_fail_with_invalid_user_info() => _result.ShouldBeOfExactType<InvalidUserInfo>();
    [Fact] void should_not_expose_the_password() => _result.Message.Contains("s3", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_expose_the_username() => _result.Message.Contains("user:", StringComparison.Ordinal).ShouldBeFalse();
}
