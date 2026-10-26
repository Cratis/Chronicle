// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static Cratis.Chronicle.Security.given.store_effect;
using static Cratis.Chronicle.Security.given.store_scoped_surface_classification;

namespace Cratis.Chronicle.Security.given;

public static class store_scoped_grpc_operations
{
    /// <summary>
    /// Matches the hand-written service contracts registered in Server/GrpcServiceRegistrations.cs.
    /// </summary>
    public static IReadOnlyList<Type> Contracts { get; } =
    [
        typeof(Chronicle.Contracts.Compliance.ICompliance),
        typeof(Chronicle.Contracts.Events.Constraints.IConstraints),
        typeof(Chronicle.Contracts.Clients.IConnectionService),
        typeof(Chronicle.Contracts.Observation.IObservers),
        typeof(Chronicle.Contracts.Observation.IFailedPartitions),
        typeof(Chronicle.Contracts.Observation.Reactors.IReactors),
        typeof(Chronicle.Contracts.Observation.Reducers.IReducers),
        typeof(Chronicle.Contracts.Observation.EventStoreSubscriptions.IEventStoreSubscriptions),
        typeof(Chronicle.Contracts.Projections.IProjections),
        typeof(Chronicle.Contracts.ReadModels.IReadModels),
        typeof(Chronicle.Contracts.ReadModels.IMaterializedReadModels),
        typeof(Chronicle.Contracts.Host.IServer)
    ];

    public static IReadOnlyDictionary<string, store_scoped_surface> All { get; } = new store_scoped_surface[]
    {
        Read("Cratis.Chronicle.Contracts.Compliance.ICompliance.Release", Exports),
        Manage("Cratis.Chronicle.Contracts.Compliance.ICompliance.DeleteEncryptionKey", Destroys),
        Manage("Cratis.Chronicle.Contracts.Compliance.ICompliance.AllowNewEncryptionKey", Configures),
        Append("Cratis.Chronicle.Contracts.Events.Constraints.IConstraints.Register", Configures),
        Manage("Cratis.Chronicle.Contracts.Observation.IObservers.Replay", Configures),
        Manage("Cratis.Chronicle.Contracts.Observation.IObservers.ReplayPartition", Configures),
        Manage("Cratis.Chronicle.Contracts.Observation.IObservers.FastForward", Configures),
        Manage("Cratis.Chronicle.Contracts.Observation.IObservers.RetryPartition", Configures),
        Manage("Cratis.Chronicle.Contracts.Observation.IObservers.ClearPartitionQuarantine", Configures),
        Manage("Cratis.Chronicle.Contracts.Observation.IObservers.ClearObserverQuarantine", Configures),
        Manage("Cratis.Chronicle.Contracts.Observation.IObservers.ClearFailedPartitions", Destroys),
        Manage("Cratis.Chronicle.Contracts.Observation.IObservers.RetryFailedPartitions", Configures),
        Manage("Cratis.Chronicle.Contracts.Observation.IObservers.RemoveObserver", Destroys),
        Read("Cratis.Chronicle.Contracts.Observation.IObservers.GetObserverInformation"),
        Read("Cratis.Chronicle.Contracts.Observation.IObservers.GetConnectedClientsForObserver", Enumerates),
        Read("Cratis.Chronicle.Contracts.Observation.IObservers.GetObservers", Enumerates),
        Read("Cratis.Chronicle.Contracts.Observation.IObservers.ObserveObservers", store_effect.Stream, Enumerates),
        Read("Cratis.Chronicle.Contracts.Observation.IObservers.WaitForCompletion"),
        Read("Cratis.Chronicle.Contracts.Observation.IObservers.GetReplayableObserversForEventTypes", Enumerates),
        Read("Cratis.Chronicle.Contracts.Observation.IFailedPartitions.GetFailedPartitions", Enumerates),
        Read("Cratis.Chronicle.Contracts.Observation.IFailedPartitions.ObserveFailedPartitions", store_effect.Stream, Enumerates),
        Observe("Cratis.Chronicle.Contracts.Observation.Reactors.IReactors.Observe"),
        Read("Cratis.Chronicle.Contracts.Observation.Reactors.IReactors.HasReactor"),
        Observe("Cratis.Chronicle.Contracts.Observation.Reducers.IReducers.Observe"),

        // All three requests use TargetEventStore; Add also carries subscriptions' SourceEventStore selectors.
        new("Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions.IEventStoreSubscriptions.Add",
            new HashSet<store_access> { store_access.Manage, store_access.Read },
            new HashSet<store_effect> { Configures, CrossStore, CreatesDelegation }),
        Manage("Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions.IEventStoreSubscriptions.Remove", Configures, CrossStore),
        Read("Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions.IEventStoreSubscriptions.GetSubscriptions", CrossStore, Enumerates),
        Append("Cratis.Chronicle.Contracts.Projections.IProjections.Register", Configures),
        Read("Cratis.Chronicle.Contracts.Projections.IProjections.GetAllDefinitions", Enumerates),
        Read("Cratis.Chronicle.Contracts.Projections.IProjections.GetAllDeclarations", Enumerates),
        Read("Cratis.Chronicle.Contracts.Projections.IProjections.Preview"),
        Manage("Cratis.Chronicle.Contracts.Projections.IProjections.Save", Configures),
        Append("Cratis.Chronicle.Contracts.ReadModels.IReadModels.RegisterMany", Configures),
        Append("Cratis.Chronicle.Contracts.ReadModels.IReadModels.RegisterSingle", Configures),
        Manage("Cratis.Chronicle.Contracts.ReadModels.IReadModels.UpdateDefinition", Configures),
        Read("Cratis.Chronicle.Contracts.ReadModels.IReadModels.GetDefinitions", Enumerates),
        Read("Cratis.Chronicle.Contracts.ReadModels.IReadModels.GetOccurrences", Enumerates),
        Read("Cratis.Chronicle.Contracts.ReadModels.IReadModels.GetInstances", Enumerates),
        Read("Cratis.Chronicle.Contracts.ReadModels.IReadModels.GetInstanceByKey"),
        Read("Cratis.Chronicle.Contracts.ReadModels.IReadModels.GetAllInstances", Enumerates),
        Read("Cratis.Chronicle.Contracts.ReadModels.IReadModels.Watch", store_effect.Stream),
        Manage("Cratis.Chronicle.Contracts.ReadModels.IReadModels.DehydrateSession", Configures),
        Read("Cratis.Chronicle.Contracts.ReadModels.IMaterializedReadModels.GetInstances", Enumerates),
        Read("Cratis.Chronicle.Contracts.ReadModels.IMaterializedReadModels.ObserveInstances", store_effect.Stream, Enumerates),
    }.ToDictionary(surface => surface.SurfaceId, StringComparer.Ordinal);
}
