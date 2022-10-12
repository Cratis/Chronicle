// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Extensions.MongoDB;
using Aksio.Cratis.Serialization;
using Aksio.Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for <see cref="IHostBuilder"/>.
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// Use MongoDB in the solution. Configures default settings for the MongoDB Driver.
    /// </summary>
    /// <param name="builder"><see cref="IHostBuilder"/> to extend.</param>
    /// <param name="types"><see cref="ITypes"/> for type discovery.</param>
    /// <param name="derivedTypes"><see cref="IDerivedTypes"/> in the system.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use.</param>
    /// <returns><see cref="IHostBuilder"/> for building continuation.</returns>
    /// <remarks>
    /// It will automatically hook up any implementations of <see cref="IBsonClassMapFor{T}"/>
    /// and <see cref="ICanFilterMongoDBConventionPacksForType"/>.
    /// </remarks>
    public static IHostBuilder UseMongoDB(this IHostBuilder builder, ITypes types, IDerivedTypes derivedTypes, JsonSerializerOptions jsonSerializerOptions)
    {
        var defaults = new MongoDBDefaults(types, derivedTypes, jsonSerializerOptions);
        builder.ConfigureServices(_ => _.AddSingleton(defaults));
        defaults.Initialize();
        return builder;
    }
}
