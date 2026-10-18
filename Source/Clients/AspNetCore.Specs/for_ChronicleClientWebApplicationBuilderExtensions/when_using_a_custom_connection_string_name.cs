// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;

namespace Cratis.Chronicle.for_ChronicleClientWebApplicationBuilderExtensions;

public class when_using_a_custom_connection_string_name : given.a_web_builder
{
    void Establish() => _builder.Configuration["ConnectionStrings:events"] = "chronicle://customhost:4321";

    void Because()
    {
        _builder.AddCratisChronicle(connectionStringName: "events");
        ResolveOptions();
    }

    [Fact] void should_use_the_named_host() => _options.ConnectionString.ServerAddress.Host.ShouldEqual("customhost");
    [Fact] void should_use_the_named_port() => _options.ConnectionString.ServerAddress.Port.ShouldEqual(4321);
    [Fact] void should_configure_the_base_options_host() => _baseOptions.ConnectionString.ServerAddress.Host.ShouldEqual("customhost");
}
