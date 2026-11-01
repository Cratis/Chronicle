// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilder;

public class when_parsing_url_with_skip_keep_alive : Specification
{
    ChronicleConnectionStringBuilder _builder;
    ChronicleConnectionString _connectionString;
    string _built;

    void Establish() => _builder = new ChronicleConnectionStringBuilder("chronicle://localhost:35000/?skipKeepAlive=true");

    void Because()
    {
        _connectionString = new ChronicleConnectionString("chronicle://localhost:35000/?skipKeepAlive=true");
        _built = _builder.Build();
    }

    [Fact] void should_have_skip_keep_alive_set_to_true() => _builder.SkipKeepAlive.ShouldBeTrue();
    [Fact] void should_expose_skip_keep_alive_on_the_connection_string() => _connectionString.SkipKeepAlive.ShouldBeTrue();
    [Fact] void should_not_emit_the_key_twice() => _built.ShouldEqual("chronicle://localhost:35000?skipKeepAlive=true");
}
