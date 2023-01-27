// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1600

using Aksio.Cratis.Kernel.Grains.Clients;

namespace Aksio.Cratis.Kernel.Server;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpClient(ConnectedClients.ConnectedClientsHttpClient).ConfigurePrimaryHttpMessageHandler(_ => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (__, ___, ____, _____) => true
        });
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseAksio();
    }
}
