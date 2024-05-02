// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.MongoDB;

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
    /// <param name="mongoDBArtifacts">Optional <see cref="IMongoDBArtifacts"/> to use. Will default to <see cref="DefaultMongoDBArtifacts"/> which discovers at runtime.</param>
    /// <param name="jsonSerializerOptions">Optional The <see cref="JsonSerializerOptions"/> to use.</param>
    /// <returns><see cref="IHostBuilder"/> for building continuation.</returns>
    /// <remarks>
    /// It will automatically hook up any implementations of <see cref="IBsonClassMapFor{T}"/>
    /// and <see cref="ICanFilterMongoDBConventionPacksForType"/>.
    /// </remarks>
    public static IHostBuilder UseMongoDB(this IHostBuilder builder, IMongoDBArtifacts? mongoDBArtifacts = default, JsonSerializerOptions? jsonSerializerOptions = default)
    {
        MongoDBDefaults.Initialize(mongoDBArtifacts, jsonSerializerOptions);
        return builder;
    }
}
