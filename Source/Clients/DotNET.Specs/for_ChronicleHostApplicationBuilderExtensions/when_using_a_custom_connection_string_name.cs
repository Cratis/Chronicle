// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.for_ChronicleHostApplicationBuilderExtensions;

public class when_using_a_custom_connection_string_name : given.a_host_builder
{
    void Establish() => _builder.Configuration["ConnectionStrings:events"] = "chronicle://customhost:4321";

    void Because()
    {
        _builder.AddCratisChronicle(configureOptions: null, configSection: null, configure: null, connectionStringName: "events");
        ResolveOptions();
    }

    [Fact] void should_use_the_named_host() => _options.ConnectionString.ServerAddress.Host.ShouldEqual("customhost");
    [Fact] void should_use_the_named_port() => _options.ConnectionString.ServerAddress.Port.ShouldEqual(4321);
}
