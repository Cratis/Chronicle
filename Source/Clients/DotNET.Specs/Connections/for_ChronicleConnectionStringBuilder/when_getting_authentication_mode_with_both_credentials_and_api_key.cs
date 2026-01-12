// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilder;

public class when_getting_authentication_mode_with_both_credentials_and_api_key : Specification
{
    ChronicleConnectionStringBuilder _builder;
    Exception _exception;

    void Establish()
    {
        _builder = new ChronicleConnectionStringBuilder
        {
            Username = "clientId",
            Password = "clientSecret",
            ApiKey = "my-api-key"
        };
    }

    void Because() => _exception = Catch.Exception(() => _ = _builder.AuthenticationMode);

    [Fact] void should_throw_ambiguous_authentication_mode() => _exception.ShouldBeOfExactType<AmbiguousAuthenticationMode>();
}
