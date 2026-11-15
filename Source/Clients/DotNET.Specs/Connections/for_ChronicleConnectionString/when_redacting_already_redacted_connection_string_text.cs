// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionString;

public class when_redacting_already_redacted_connection_string_text : Specification
{
    string _once;
    string _twice;

    void Because()
    {
        _once = ChronicleConnectionString.Redact("chronicle://user:s3cret@host:35000");
        _twice = ChronicleConnectionString.Redact(_once);
    }

    [Fact] void should_give_the_same_result() => _twice.ShouldEqual(_once);
}
