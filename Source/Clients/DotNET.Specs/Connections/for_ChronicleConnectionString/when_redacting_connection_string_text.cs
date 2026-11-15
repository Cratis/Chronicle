// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionString;

public class when_redacting_connection_string_text : Specification
{
    string _result;

    void Because() => _result = ChronicleConnectionString.Redact("chronicle://user:s3cret@host:35000");

    [Fact] void should_mask_the_user_information() => _result.ShouldEqual("chronicle://REDACTED:REDACTED@host:35000");
}
