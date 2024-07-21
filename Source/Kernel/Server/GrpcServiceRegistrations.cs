// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    public static void AddGrpcServices(this IServiceCollection services)
    {
        services.AddSingleton<Contracts.EventSequences.IEventSequences, Services.EventSequences.EventSequences>();
        services.AddSingleton<Contracts.Events.IEventTypes, Services.Events.EventTypes>();
        services.AddSingleton<Contracts.Clients.IConnectionService, Services.Clients.ConnectionService>();
        services.AddSingleton<Contracts.Observation.IObservers, Services.Observation.Observers>();
        services.AddSingleton<Contracts.Observation.Reactions.IReactions, Services.Observation.Reactions.Reactions>();
    }

    /// <summary>
    /// Map all Grpc services and expose them on the endpoint.
    /// </summary>
    /// <param name="app"><see cref="IApplicationBuilder"/> to add to.</param>
    public static void MapGrpcServices(this IApplicationBuilder app)
    {
        app.UseEndpoints(_ =>
        {
            _.MapGrpcService<Services.EventSequences.EventSequences>();
            _.MapGrpcService<Services.Events.EventTypes>();
            _.MapGrpcService<Services.Clients.ConnectionService>();
            _.MapGrpcService<Services.Observation.Observers>();
            _.MapGrpcService<Services.Observation.Reactions.Reactions>();
        });
    }
}
