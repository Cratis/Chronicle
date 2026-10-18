// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.for_ChronicleClientWebApplicationBuilderExtensions.given;

public class a_web_builder : Specification
{
    protected WebApplicationBuilder _builder;
    protected ChronicleAspNetCoreOptions _options;
    protected ChronicleOptions _baseOptions;

    void Establish()
    {
        _builder = WebApplication.CreateBuilder();
        _builder.Configuration.Sources.Clear();
        _builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:chronicle"] = "chronicle://somehost:1234"
        });
    }

    protected void ResolveOptions()
    {
        using var services = _builder.Services.BuildServiceProvider();
        _options = services.GetRequiredService<IOptions<ChronicleAspNetCoreOptions>>().Value;
        _baseOptions = services.GetRequiredService<IOptions<ChronicleOptions>>().Value;
    }
}
