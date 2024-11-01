// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1600

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Api.Server;

/// <summary>
/// Holds extensions for configuring API for the <see cref="IServiceCollection"/>.
/// </summary>
public static class ApiServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Cratis Chronicle API to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddCratisChronicleApi(this IServiceCollection services)
    {
        services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            });

        services.AddSwaggerGen(options =>
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, typeof(Startup).Assembly.GetName().Name + ".xml");
            options.IncludeXmlComments(filePath);
        });
        return services;
    }
}
