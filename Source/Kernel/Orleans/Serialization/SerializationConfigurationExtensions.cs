// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Configuration;
using Orleans.Hosting;

namespace Aksio.Cratis.Kernel.Orleans.Serialization;

/// <summary>
/// Extension methods for configuring serialization.
/// </summary>
public static class SerializationConfigurationExtensions
{
    /// <summary>
    /// Configure serialization for Orleans.
    /// </summary>
    /// <param name="siloBuilder"><see cref="ISiloBuilder"/> to configure for.</param>
    public static ISiloBuilder ConfigureSerialization(this ISiloBuilder siloBuilder)
    {
        siloBuilder.Configure<SerializationProviderOptions>(options => options.SerializationProviders.Add(typeof(ExpandoObjectSerializer)));
        return siloBuilder;
    }
}
