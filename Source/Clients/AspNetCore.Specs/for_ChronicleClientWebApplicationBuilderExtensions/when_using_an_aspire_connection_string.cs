// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Microsoft.AspNetCore.Builder;

namespace Cratis.Chronicle.for_ChronicleClientWebApplicationBuilderExtensions;

public class when_using_an_aspire_connection_string : given.a_web_builder
{
    void Because()
    {
        _builder.AddCratisChronicle();
        ResolveOptions();
    }

    [Fact] void should_use_the_referenced_host() => _options.ConnectionString.ServerAddress.Host.ShouldEqual("somehost");
    [Fact] void should_use_the_referenced_port() => _options.ConnectionString.ServerAddress.Port.ShouldEqual(1234);
    [Fact] void should_configure_the_base_options_host() => _baseOptions.ConnectionString.ServerAddress.Host.ShouldEqual("somehost");
    [Fact] void should_configure_the_base_options_port() => _baseOptions.ConnectionString.ServerAddress.Port.ShouldEqual(1234);
    [Fact] void should_use_the_existing_client_credentials_flow() => _options.ConnectionString.AuthenticationMode.ShouldEqual(AuthenticationMode.ClientCredentials);
}
