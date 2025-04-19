// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Extension methods for working with the Grpc services.
/// </summary>
public static class GrpcServiceRegistrations
{
    /// <summary>
    /// Add all Grpc services to the service collection.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddGrpcServices(this IServiceCollection services)
    {
        services.AddGrpc(options => options.Interceptors.Add<CorrelationIdServerInterceptor>());
        services.AddSingleton<Contracts.IEventStores, Services.EventStores>();
        services.AddSingleton<Contracts.INamespaces, Services.Namespaces>();
        services.AddSingleton<Contracts.Recommendations.IRecommendations, Services.Recommendations.Recommendations>();
        services.AddSingleton<Contracts.Identities.IIdentities, Services.Identities.Identities>();
        services.AddSingleton<Contracts.EventSequences.IEventSequences, Services.EventSequences.EventSequences>();
        services.AddSingleton<Contracts.Events.IEventTypes, Services.Events.EventTypes>();
        services.AddSingleton<Contracts.Events.Constraints.IConstraints, Services.Events.Constraints.Constraints>();
        services.AddSingleton<Contracts.Clients.IConnectionService, Services.Clients.ConnectionService>();
        services.AddSingleton<Contracts.Observation.IObservers, Services.Observation.Observers>();
        services.AddSingleton<Contracts.Observation.IFailedPartitions, Services.Observation.FailedPartitions>();
        services.AddSingleton<Contracts.Observation.Reactors.IReactors, Services.Observation.Reactors.Reactors>();
        services.AddSingleton<Contracts.Observation.Reducers.IReducers, Services.Observation.Reducers.Reducers>();
        services.AddSingleton<Contracts.Projections.IProjections, Services.Projections.Projections>();
        services.AddSingleton<Contracts.Jobs.IJobs, Services.Jobs.Jobs>();
        services.AddSingleton<Contracts.Host.IServer, Services.Host.Server>();

        return services;
    }

    /// <summary>
    /// Map all Grpc services and expose them on the endpoint.
    /// </summary>
    /// <param name="app"><see cref="IApplicationBuilder"/> to add to.</param>
    /// <returns><see cref="IApplicationBuilder"/> for continuation.</returns>
    public static IApplicationBuilder MapGrpcServices(this IApplicationBuilder app)
    {
        app.UseEndpoints(_ =>
        {
            _.MapGrpcService<Services.EventStores>();
            _.MapGrpcService<Services.Namespaces>();
            _.MapGrpcService<Services.Recommendations.Recommendations>();
            _.MapGrpcService<Services.Identities.Identities>();
            _.MapGrpcService<Services.EventSequences.EventSequences>();
            _.MapGrpcService<Services.Events.EventTypes>();
            _.MapGrpcService<Services.Events.Constraints.Constraints>();
            _.MapGrpcService<Services.Clients.ConnectionService>();
            _.MapGrpcService<Services.Observation.Observers>();
            _.MapGrpcService<Services.Observation.FailedPartitions>();
            _.MapGrpcService<Services.Observation.Reactors.Reactors>();
            _.MapGrpcService<Services.Observation.Reducers.Reducers>();
            _.MapGrpcService<Services.Projections.Projections>();
            _.MapGrpcService<Services.Jobs.Jobs>();
            _.MapGrpcService<Services.Host.Server>();
        });

        return app;
    }
}
