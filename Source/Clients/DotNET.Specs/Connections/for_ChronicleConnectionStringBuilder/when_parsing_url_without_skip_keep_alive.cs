// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilder;

public class when_parsing_url_without_skip_keep_alive : Specification
{
    ChronicleConnectionStringBuilder _builder;

    void Establish() => _builder = new ChronicleConnectionStringBuilder("chronicle://localhost:35000");

    [Fact] void should_not_skip_keep_alive() => _builder.SkipKeepAlive.ShouldBeFalse();
}
