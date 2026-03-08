// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts;

/// <summary>
/// Defines all the service contract types registered in Chronicle.
/// </summary>
public static class RegisteredServiceTypes
{
    /// <summary>
    /// Gets all service types that are registered.
    /// </summary>
    public static Type[] All { get; } =
    [
        typeof(IEventStores),
        typeof(INamespaces),
        typeof(Recommendations.IRecommendations),
        typeof(Identities.IIdentities),
        typeof(EventSequences.IEventSequences),
        typeof(Events.IEventTypes),
        typeof(Events.Constraints.IConstraints),
        typeof(Clients.IConnectionService),
        typeof(Observation.IObservers),
        typeof(Observation.IFailedPartitions),
        typeof(Observation.Reactors.IReactors),
        typeof(Observation.Reducers.IReducers),
        typeof(Observation.Webhooks.IWebhooks),
        typeof(Projections.IProjections),
        typeof(ReadModels.IReadModels),
        typeof(Jobs.IJobs),
        typeof(Seeding.IEventSeeding),
        typeof(Security.IUsers),
        typeof(Security.IApplications),
        typeof(Host.IServer)
    ];
}
