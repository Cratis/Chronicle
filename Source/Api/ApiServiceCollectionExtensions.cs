// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1600

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.XPath;
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
using Grpc.Net.Client;
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
    /// <param name="addGrpc">Whether or not to add gRPC services configuration.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddCratisChronicleApi(this IServiceCollection services, bool addGrpc = true)
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

        if (addGrpc)
        {
            services.AddSingleton<GrpcConnectionManager>();
            services.AddSingleton(sp => sp.GetRequiredService<GrpcConnectionManager>().Channel);
            services.AddSingleton(sp => sp.GetRequiredService<GrpcChannel>().CreateGrpcService<IEventStores>());
            services.AddSingleton(sp => sp.GetRequiredService<GrpcChannel>().CreateGrpcService<INamespaces>());
            services.AddSingleton(sp => sp.GetRequiredService<GrpcChannel>().CreateGrpcService<IEventSequences>());
            services.AddSingleton(sp => sp.GetRequiredService<GrpcChannel>().CreateGrpcService<IEventTypes>());
            services.AddSingleton(sp => sp.GetRequiredService<GrpcChannel>().CreateGrpcService<IConstraints>());
            services.AddSingleton(sp => sp.GetRequiredService<GrpcChannel>().CreateGrpcService<IObservers>());
            services.AddSingleton(sp => sp.GetRequiredService<GrpcChannel>().CreateGrpcService<IReactors>());
            services.AddSingleton(sp => sp.GetRequiredService<GrpcChannel>().CreateGrpcService<IReducers>());
            services.AddSingleton(sp => sp.GetRequiredService<GrpcChannel>().CreateGrpcService<IProjections>());
            services.AddSingleton(sp => sp.GetRequiredService<GrpcChannel>().CreateGrpcService<IJobs>());
            services.AddSingleton(sp => sp.GetRequiredService<GrpcChannel>().CreateGrpcService<IServer>());
        }
        return services;
    }
}
