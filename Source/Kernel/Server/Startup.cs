// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Connections;
using Aksio.Cratis.Events.Observation;
using Aksio.Cratis.Events.Projections;
using Aksio.Cratis.Extensions.Orleans.Execution;

#pragma warning disable SA1600

namespace Aksio.Cratis.Server;

public class Startup
{
    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseAksio();

        // TODO: This needs to be improved.
        // In a regular client, this is hooked up with a hosted service, that is too early within the kernel
        app.ApplicationServices.GetService<IConnectionManager>()!.SetKernelMode();
        app.ApplicationServices.GetService<IRequestContextManager>()!.Set(RequestContextKeys.ConnectionId, ConnectionId.Kernel);
        app.ApplicationServices.GetService<IProjectionsRegistrar>()!.StartAll().Wait();
        app.ApplicationServices.GetService<IObservers>()!.RegisterAndObserveAll().Wait();
    }
}
