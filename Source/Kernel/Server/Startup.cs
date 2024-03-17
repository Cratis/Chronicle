// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1600

using Cratis.Kernel.Grains.Clients;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Grpc.Server;

namespace Cratis.Kernel.Server;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddCodeFirstGrpc();
        services.AddGrpcServices();
        services.AddHttpClient(ConnectedClients.ConnectedClientsHttpClient).ConfigurePrimaryHttpMessageHandler(_ => new HttpClientHandler
        {
#pragma warning disable MA0039 // Allowing self-signed certificates for clients connecting to the Kernel
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        });

        services.AddSingleton(BinderConfiguration.Default);
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseWebSockets();
        app.MapGrpcServices();
        app.UseCratis();
    }
}
