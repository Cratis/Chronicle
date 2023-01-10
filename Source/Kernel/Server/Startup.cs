// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Observation;
using Aksio.Cratis.Events.Projections;

#pragma warning disable SA1600

namespace Aksio.Cratis.Server;

public class Startup
{
    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseAksio();
    }
}
