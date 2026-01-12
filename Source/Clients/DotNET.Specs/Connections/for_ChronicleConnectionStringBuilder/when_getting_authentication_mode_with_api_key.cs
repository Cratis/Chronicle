// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilder;

public class when_getting_authentication_mode_with_api_key : Specification
{
    ChronicleConnectionStringBuilder _builder;
    AuthenticationMode _mode;

    void Establish()
    {
        _builder = new ChronicleConnectionStringBuilder
        {
            ApiKey = "my-api-key"
        };
    }

    void Because() => _mode = _builder.AuthenticationMode;

    [Fact] void should_be_api_key() => _mode.ShouldEqual(AuthenticationMode.ApiKey);
}
