// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Serialization;
using Orleans.TestingHost;

namespace Aksio.Cratis.Kernel.Grains;

public class OrleansClusterConfigurations : ISiloConfigurator
{
    public void Configure(ISiloBuilder siloBuilder)
    {
        siloBuilder.ConfigureSerialization();
    }
}
