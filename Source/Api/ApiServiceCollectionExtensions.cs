// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1600

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.XPath;
using Cratis.Applications.Swagger;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Contracts.Events.Constraints;
using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Contracts.Host;
using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Execution;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc.Client;

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
    /// <param name="chronicleServices">Optional <see cref="IServices"/> for the Chronicle services.</param>
    /// <param name="useGrpc">Whether to use gRPC or not.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    /// <remarks>
    /// Typically the optional <see cref="IServices"/> is used when running everything in process, or one wants
    /// to reuse the services that is already configured for the client.
    /// </remarks>
    public static IServiceCollection AddCratisChronicleApi(this IServiceCollection services, IServices? chronicleServices = null, bool useGrpc = true)
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

            options.AddConcepts();
        });

        if (chronicleServices is null || useGrpc)
        {
            services.AddSingleton<IChronicleConnection>(sp =>
            {
                var lifetime = sp.GetRequiredService<IHostApplicationLifetime>();
                var options = sp.GetRequiredService<IOptions<ChronicleApiOptions>>();
                var correlationIdAccessor = sp.GetRequiredService<ICorrelationIdAccessor>();
                var connectionLifecycle = new ConnectionLifecycle(sp.GetRequiredService<ILogger<ConnectionLifecycle>>());
                return new ChronicleConnection(
                    options.Value.ChronicleUrl,
                    options.Value.ConnectTimeout,
                    connectionLifecycle,
                    new Cratis.Tasks.TaskFactory(),
                    correlationIdAccessor,
                    sp.GetRequiredService<ILogger<ChronicleConnection>>(),
                    lifetime.ApplicationStopping);
            });
            services.AddSingleton(sp => sp.GetRequiredService<IChronicleConnection>().Services);
        }

        if (chronicleServices is not null)
        {
            services.AddSingleton(chronicleServices);
        }

        services.AddSingleton(sp => sp.GetRequiredService<IServices>().EventStores);
        services.AddSingleton(sp => sp.GetRequiredService<IServices>().Namespaces);
        services.AddSingleton(sp => sp.GetRequiredService<IServices>().Recommendations);
        services.AddSingleton(sp => sp.GetRequiredService<IServices>().Identities);
        services.AddSingleton(sp => sp.GetRequiredService<IServices>().EventSequences);
        services.AddSingleton(sp => sp.GetRequiredService<IServices>().EventTypes);
        services.AddSingleton(sp => sp.GetRequiredService<IServices>().Constraints);
        services.AddSingleton(sp => sp.GetRequiredService<IServices>().Observers);
        services.AddSingleton(sp => sp.GetRequiredService<IServices>().FailedPartitions);
        services.AddSingleton(sp => sp.GetRequiredService<IServices>().Reactors);
        services.AddSingleton(sp => sp.GetRequiredService<IServices>().Reducers);
        services.AddSingleton(sp => sp.GetRequiredService<IServices>().Projections);
        services.AddSingleton(sp => sp.GetRequiredService<IServices>().Jobs);

        return services;
    }
}
