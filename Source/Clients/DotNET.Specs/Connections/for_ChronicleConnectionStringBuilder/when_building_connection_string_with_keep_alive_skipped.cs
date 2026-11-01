// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilder;

public class when_building_connection_string_with_keep_alive_skipped : Specification
{
    ChronicleConnectionStringBuilder _builder;
    string _url;
    ChronicleConnectionStringBuilder _parsed;
    string _urlWithoutSkip;

    void Establish()
    {
        _builder = new ChronicleConnectionStringBuilder
        {
            Host = "localhost",
            Port = 35000,
            SkipKeepAlive = true
        };
    }

    void Because()
    {
        _url = _builder.Build();
        _parsed = new ChronicleConnectionStringBuilder(_url);
        _urlWithoutSkip = new ChronicleConnectionStringBuilder { Host = "localhost", Port = 35000, SkipKeepAlive = false }.Build();
    }

    [Fact] void should_include_skip_keep_alive_in_query_string() => _url.ShouldEqual("chronicle://localhost:35000?skipKeepAlive=true");
    [Fact] void should_round_trip() => _parsed.SkipKeepAlive.ShouldBeTrue();
    [Fact] void should_not_emit_false() => _urlWithoutSkip.ShouldEqual("chronicle://localhost:35000");
}
