// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Grpc.AspNetCore.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.for_GrpcServiceRegistrations.given;

/// <summary>
/// Resolves the gRPC server options a kernel configured with the given <see cref="ChronicleOptions"/> ends up with.
/// </summary>
public static class the_grpc_options
{
    public static GrpcServiceOptions For(ChronicleOptions chronicle)
    {
        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(chronicle));
        services.AddGrpcServices();
        return services.BuildServiceProvider().GetRequiredService<IOptions<GrpcServiceOptions>>().Value;
    }
}
