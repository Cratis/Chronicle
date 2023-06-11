// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Sample;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.UseMongoDBReadModels();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseCratis();
        app.UseAksio();
    }
}
