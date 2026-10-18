// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;

namespace Cratis.Chronicle.for_ChronicleClientWebApplicationBuilderExtensions;

public class when_the_callback_overrides_an_invalid_named_connection_string : given.a_web_builder
{
    void Establish() => _builder.Configuration["ConnectionStrings:chronicle"] = "invalid";

    void Because()
    {
        _builder.AddCratisChronicle(options => options.ConnectionString = new("chronicle://callbackhost:9876"));
        ResolveOptions();
    }

    [Fact] void should_use_the_callback_host() => _options.ConnectionString.ServerAddress.Host.ShouldEqual("callbackhost");
    [Fact] void should_use_the_callback_port() => _options.ConnectionString.ServerAddress.Port.ShouldEqual(9876);
    [Fact] void should_configure_the_base_options_host() => _baseOptions.ConnectionString.ServerAddress.Host.ShouldEqual("callbackhost");
    [Fact] void should_configure_the_base_options_port() => _baseOptions.ConnectionString.ServerAddress.Port.ShouldEqual(9876);
}
