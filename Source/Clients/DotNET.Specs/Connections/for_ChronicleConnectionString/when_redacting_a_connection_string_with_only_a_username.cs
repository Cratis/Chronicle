// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionString;

public class when_redacting_a_connection_string_with_only_a_username : Specification
{
    string _result;

    void Because() => _result = new ChronicleConnectionString("chronicle://user@host:35000").Redacted;

    [Fact] void should_not_expose_the_username() => _result.Contains("//user", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_mask_the_user_information() => _result.ShouldEqual("chronicle://REDACTED@host:35000");
}
