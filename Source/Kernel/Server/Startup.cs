// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1600

using Cratis.Api.Server;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Grpc.Server;

namespace Cratis.Chronicle.Server;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddCodeFirstGrpc();
        services.AddCratisChronicleApi();
        services.AddGrpcServices();
        services.AddSingleton(BinderConfiguration.Default);
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseCratisChronicleApi();
        app.MapGrpcServices();
    }
}
