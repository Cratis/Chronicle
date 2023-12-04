// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Serialization;
using Microsoft.Extensions.Configuration;
using Orleans.TestingHost;

namespace Aksio.Cratis.Kernel.Grains;

public class OrleansClientConfigurations : IClientBuilderConfigurator
{
    public void Configure(IConfiguration configuration, global::Orleans.Hosting.IClientBuilder clientBuilder)
    {
        clientBuilder.ConfigureSerialization();
    }
}
