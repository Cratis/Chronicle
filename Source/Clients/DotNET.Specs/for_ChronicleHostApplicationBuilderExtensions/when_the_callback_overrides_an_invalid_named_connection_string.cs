// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.for_ChronicleHostApplicationBuilderExtensions;

public class when_the_callback_overrides_an_invalid_named_connection_string : given.a_host_builder
{
    void Establish() => _builder.Configuration["ConnectionStrings:chronicle"] = "invalid";

    void Because()
    {
        _builder.AddCratisChronicle(options => options.ConnectionString = new("chronicle://callbackhost:9876"));
        ResolveOptions();
    }

    [Fact] void should_use_the_callback_host() => _options.ConnectionString.ServerAddress.Host.ShouldEqual("callbackhost");
    [Fact] void should_use_the_callback_port() => _options.ConnectionString.ServerAddress.Port.ShouldEqual(9876);
}
