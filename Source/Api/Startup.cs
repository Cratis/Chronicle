// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1600

namespace Cratis.Api.Server;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddCratisChronicleApi();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseCratisApplicationModel();
    }
}
