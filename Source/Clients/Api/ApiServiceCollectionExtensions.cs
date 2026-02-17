// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1600

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.XPath;
using Cratis.Arc.Swagger;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Identities;
using Cratis.Execution;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Api;

/// <summary>
/// Holds extensions for configuring API for the <see cref="IServiceCollection"/>.
/// </summary>
public static class ApiServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Cratis Chronicle API to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <param name="useGrpc">Whether to use gRPC or not.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    /// <remarks>
    /// Typically the optional <see cref="IServices"/> is used when running everything in process, or one wants
    /// to reuse the services that is already configured for the client.
    /// </remarks>
    public static IServiceCollection AddCratisChronicleApi(this IServiceCollection services, bool useGrpc = true)
    {
        services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            });
        services.TryAddSingleton<ICorrelationIdAccessor, CorrelationIdAccessor>();
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

            options.AddConcepts();
        });

        var hasServices = services.Any(s => s.ServiceType == typeof(IServices));
        if (!hasServices || useGrpc)
        {
            services.AddCratisChronicleConnection(connectionStringFactory: sp =>
            {
                var options = sp.GetRequiredService<IOptions<ChronicleOptions>>();
                return options.Value.ConnectionString;
            });
        }

        services.AddCratisChronicleServices();
        services.AddCausation();

        services.AddSingleton<IControllerActivator, CustomControllerActivator>();

        return services;
    }
}
