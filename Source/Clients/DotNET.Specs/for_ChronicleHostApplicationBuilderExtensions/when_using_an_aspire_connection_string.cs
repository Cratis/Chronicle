// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.for_ChronicleHostApplicationBuilderExtensions;

public class when_using_an_aspire_connection_string : given.a_host_builder
{
    void Because()
    {
        _builder.AddCratisChronicle();
        ResolveOptions();
    }

    [Fact] void should_use_the_referenced_host() => _options.ConnectionString.ServerAddress.Host.ShouldEqual("somehost");
    [Fact] void should_use_the_referenced_port() => _options.ConnectionString.ServerAddress.Port.ShouldEqual(1234);
    [Fact] void should_use_the_existing_client_credentials_flow() => _options.ConnectionString.AuthenticationMode.ShouldEqual(AuthenticationMode.ClientCredentials);
    [Fact] void should_allow_the_existing_development_username_fallback() => _options.ConnectionString.Username.ShouldBeNull();
    [Fact] void should_allow_the_existing_development_password_fallback() => _options.ConnectionString.Password.ShouldBeNull();
}
