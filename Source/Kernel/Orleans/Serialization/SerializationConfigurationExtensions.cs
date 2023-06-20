// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Json;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Serialization;

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
    /// <returns><see cref="ISiloBuilder"/> for continuation.</returns>
    public static ISiloBuilder ConfigureSerialization(this ISiloBuilder siloBuilder)
    {
        siloBuilder.Services.AddSerializer(serializerBuilder => serializerBuilder.AddJsonSerializer(_ => _ == typeof(JsonObject) || (_.Namespace?.StartsWith("Aksio") ?? false), Globals.JsonSerializerOptions));

        //throw new NotImplementedException();
        // siloBuilder.Configure<SerializationProviderOptions>(options => options.SerializationProviders.Add(typeof(ExpandoObjectSerializer)));
        return siloBuilder;
    }
}
