// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilderExtensions;

public class when_using_build : Specification
{
    ChronicleConnectionStringBuilder _builder;
    string _result;

    void Establish()
    {
        _builder = new ChronicleConnectionStringBuilder
        {
            Host = "example.com",
            Port = 8080
        };
    }

    void Because() => _result = ChronicleConnectionStringBuilderExtensions.Build(_builder);

    [Fact] void should_return_built_url() => _result.ShouldEqual("chronicle://example.com:8080");
}
