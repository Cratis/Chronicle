// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.for_ChronicleHostApplicationBuilderExtensions;

public class when_using_a_custom_configuration_section : given.a_host_builder
{
    void Establish() => _builder.Configuration["MyChronicle:ConnectionString"] = "chronicle://sectionhost:6789";

    void Because()
    {
        _builder.AddCratisChronicle(configSection: "MyChronicle");
        ResolveOptions();
    }

    [Fact] void should_use_the_custom_section_host() => _options.ConnectionString.ServerAddress.Host.ShouldEqual("sectionhost");
    [Fact] void should_use_the_custom_section_port() => _options.ConnectionString.ServerAddress.Port.ShouldEqual(6789);
}
