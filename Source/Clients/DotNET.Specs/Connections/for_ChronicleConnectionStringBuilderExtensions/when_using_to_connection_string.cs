// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilderExtensions;

public class when_using_to_connection_string : Specification
{
    ChronicleConnectionStringBuilder _builder;
    ChronicleConnectionString _result;

    void Establish()
    {
        _builder = new ChronicleConnectionStringBuilder
        {
            Host = "example.com",
            Port = 8080
        };
    }

    void Because() => _result = _builder.ToConnectionString();

    [Fact] void should_return_connection_string_instance() => _result.ShouldNotBeNull();
    [Fact] void should_have_correct_host() => _result.ServerAddress.Host.ShouldEqual("example.com");
    [Fact] void should_have_correct_port() => _result.ServerAddress.Port.ShouldEqual(8080);
}
