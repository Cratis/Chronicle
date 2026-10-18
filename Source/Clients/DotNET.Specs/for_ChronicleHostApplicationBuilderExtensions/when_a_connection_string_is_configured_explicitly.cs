// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.for_ChronicleHostApplicationBuilderExtensions;

public class when_a_connection_string_is_configured_explicitly : given.a_host_builder
{
    void Establish()
    {
        _builder.Configuration["Cratis:Chronicle:ConnectionString"] = "chronicle://configuredhost:5678";
        _builder.Configuration["ConnectionStrings:chronicle"] = "not a valid connection string";
    }

    void Because()
    {
        _builder.AddCratisChronicle();
        ResolveOptions();
    }

    [Fact] void should_use_the_explicit_host() => _options.ConnectionString.ServerAddress.Host.ShouldEqual("configuredhost");
    [Fact] void should_use_the_explicit_port() => _options.ConnectionString.ServerAddress.Port.ShouldEqual(5678);
}
