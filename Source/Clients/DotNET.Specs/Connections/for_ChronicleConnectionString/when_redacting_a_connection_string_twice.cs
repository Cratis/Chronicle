// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionString;

public class when_redacting_a_connection_string_twice : Specification
{
    string _first;
    string _second;

    void Because()
    {
        var connectionString = new ChronicleConnectionString("chronicle://user:s3cret@host:35000");
        _first = connectionString.Redacted;
        _second = connectionString.Redacted;
    }

    [Fact] void should_return_the_same_instance() => ReferenceEquals(_first, _second).ShouldBeTrue();
}
