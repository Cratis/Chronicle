// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Configuration;

namespace Cratis.Chronicle.Server.for_ClusterEndpointConfiguration.given;

public class a_silo : Specification
{
    protected static EndpointOptions Configure(Clustering clustering) => ConfigureOrleans(silo => ClusterEndpointConfiguration.Configure(silo, clustering));

    protected static EndpointOptions ConfigureOrleans(Action<ISiloBuilder> configure)
    {
        var services = new ServiceCollection();
        var silo = Substitute.For<ISiloBuilder>();
        silo.Services.Returns(services);
        configure(silo);
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<EndpointOptions>>().Value;
    }
}
