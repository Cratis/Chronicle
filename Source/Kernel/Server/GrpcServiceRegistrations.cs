// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf.Grpc.Server;

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
        services.AddCodeFirstGrpc(options => options.EnableDetailedErrors = true);
        services.AddGeneratedGrpcServices();

        // Everything below is still hand-written, either because the area has not been converted to Arc
        // artifacts yet or because it cannot be - see NonDerivedGrpcServices in Core.csproj, and the
        // streaming services, whose server-to-client lifetime no command or query describes.
        services.AddSingleton<Contracts.Compliance.ICompliance, Services.Compliance.ComplianceService>();
        services.AddSingleton<Contracts.Events.Constraints.IConstraints, Services.Events.Constraints.Constraints>();
        services.AddSingleton<Contracts.Clients.IConnectionService, Services.Clients.ConnectionService>();
        services.AddSingleton<Contracts.Observation.IObservers, Services.Observation.Observers>();
        services.AddSingleton<Contracts.Observation.IFailedPartitions, Services.Observation.FailedPartitions>();
        services.AddSingleton<Contracts.Observation.Reactors.IReactors, Services.Observation.Reactors.Reactors>();
        services.AddSingleton<Contracts.Observation.Reducers.IReducers, Services.Observation.Reducers.Reducers>();
        services.AddSingleton<Contracts.Observation.EventStoreSubscriptions.IEventStoreSubscriptions, Services.Observation.EventStoreSubscriptions.EventStoreSubscriptions>();
        services.AddSingleton<Contracts.Projections.IProjections, Services.Projections.Projections>();
        services.AddSingleton<Contracts.ReadModels.IReadModels, Services.ReadModels.ReadModels>();
        services.AddSingleton<Contracts.ReadModels.IMaterializedReadModels, Services.ReadModels.MaterializedReadModels>();
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
            _.MapGeneratedGrpcServices();

            _.MapGrpcService<Services.Compliance.ComplianceService>();
            _.MapGrpcService<Services.Events.Constraints.Constraints>();
            _.MapGrpcService<Services.Clients.ConnectionService>();
            _.MapGrpcService<Services.Observation.Observers>();
            _.MapGrpcService<Services.Observation.FailedPartitions>();
            _.MapGrpcService<Services.Observation.Reactors.Reactors>();
            _.MapGrpcService<Services.Observation.Reducers.Reducers>();
            _.MapGrpcService<Services.Observation.EventStoreSubscriptions.EventStoreSubscriptions>();
            _.MapGrpcService<Services.Projections.Projections>();
            _.MapGrpcService<Services.ReadModels.ReadModels>();
            _.MapGrpcService<Services.ReadModels.MaterializedReadModels>();
            _.MapGrpcService<Services.Host.Server>();
        });

        return app;
    }
}
