// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.for_ChronicleHostApplicationBuilderExtensions.given;

public class a_host_builder : Specification
{
    protected HostApplicationBuilder _builder;
    protected ChronicleClientOptions _options;

    void Establish()
    {
        _builder = Host.CreateApplicationBuilder();
        _builder.Configuration.Sources.Clear();
        _builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:chronicle"] = "chronicle://somehost:1234"
        });
    }

    protected void ResolveOptions()
    {
        using var services = _builder.Services.BuildServiceProvider();
        _options = services.GetRequiredService<IOptions<ChronicleClientOptions>>().Value;
    }
}
