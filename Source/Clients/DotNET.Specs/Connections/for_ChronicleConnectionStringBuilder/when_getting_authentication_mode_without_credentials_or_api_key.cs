// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilder;

public class when_getting_authentication_mode_without_credentials_or_api_key : Specification
{
    ChronicleConnectionStringBuilder _builder;
    Exception _exception;

    void Establish() => _builder = new ChronicleConnectionStringBuilder();

    void Because() => _exception = Catch.Exception(() => _ = _builder.AuthenticationMode);

    [Fact] void should_throw_missing_authentication() => _exception.ShouldBeOfExactType<MissingAuthentication>();
}
