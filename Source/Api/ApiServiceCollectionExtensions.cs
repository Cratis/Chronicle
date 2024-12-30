// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1600

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.XPath;

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
            options.IncludeXmlComments(() =>
            {
                var type = typeof(ApiServiceCollectionExtensions);
                var resourceName = $"{type.Assembly.GetName().Name}.xml";
                var stream = type.Assembly.GetManifestResourceStream(resourceName);
                var reader = XmlReader.Create(stream!);
                return new XPathDocument(reader);
            });
        });
        return services;
    }
}
