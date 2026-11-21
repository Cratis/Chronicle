// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Workbench.for_WorkbenchWebApplicationBuilderExtensions;

public class when_a_valid_address_is_configured : Specification
{
    ChronicleWorkbenchOptions _options;

    void Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cratis:Chronicle:Workbench:Address"] = "localhost",
            ["Cratis:Chronicle:Workbench:Port"] = "9877"
        });
        builder.UseCratisChronicleWorkbench();
        using var services = builder.Services.BuildServiceProvider();
        _options = services.GetRequiredService<IOptions<ChronicleWorkbenchOptions>>().Value;
    }

    [Fact] void should_bind_the_address() => _options.Address.ShouldEqual("localhost");
    [Fact] void should_bind_the_port() => _options.Port.ShouldEqual(9877);
}
