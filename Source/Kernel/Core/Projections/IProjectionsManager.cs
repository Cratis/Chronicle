// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Orleans.Concurrency;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines a system that is responsible for supervises projections in the system.
/// </summary>
public interface IProjectionsManager : IGrainWithStringKey
{
    /// <summary>
    /// Ensure the existence of the projections manager.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// A no-op call whose only purpose is activating the grain, so it must not queue FIFO behind whatever
    /// else is already pending on this non-reentrant grain (typically a client Register() fan-out during a
    /// rolling redeploy) - the silo's own startup path calls this and has no resilience against waiting on
    /// a busy queue for work this call never does (#3848).
    /// </remarks>
    [AlwaysInterleave]
    Task Ensure();

    /// <summary>
    /// Ensure healthy active projections are subscribed in the given namespace, joining any pending pass for it.
    /// </summary>
    /// <param name="namespace">The namespace to reconcile.</param>
    /// <returns>A task that completes after healthy subscriptions; retired observers are skipped and artifact failures are logged for retry.</returns>
    [AlwaysInterleave]
    Task EnsureSubscribedIn(EventStoreNamespaceName @namespace);

    /// <summary>
    /// Get all the <see cref="ProjectionDefinition">projection definitions</see> available.
    /// </summary>
    /// <returns>A collection of <see cref="ProjectionDefinition"/>.</returns>
    [AlwaysInterleave]
    Task<IEnumerable<ProjectionDefinition>> GetProjectionDefinitions();

    /// <summary>
    /// Get all the projections with their declaration representation.
    /// </summary>
    /// <returns>A collection of <see cref="ProjectionWithDeclaration"/>.</returns>
    Task<IEnumerable<ProjectionWithDeclaration>> GetProjectionDeclarations();

    /// <summary>
    /// Register a set of <see cref="ProjectionDefinition"/> for the event store it belongs to.
    /// </summary>
    /// <param name="definitions">A collection of <see cref="ProjectionDefinition"/>.</param>
    /// <param name="fullSetOwner">
    /// When set, the registration is the complete set of projections for that <see cref="ProjectionOwner"/> and any
    /// registered projection with the same owner that is not in the set is retired: its observer is unsubscribed in
    /// every namespace, its jobs and failed partitions are cleared, its definition is removed from the engine and from
    /// storage, and its sink container is left untouched. Leave unset for a partial registration (for example saving a
    /// single projection), which must never retire anything.
    /// </param>
    /// <param name="registrant">
    /// Optional identity of the application registering. It is recorded per projection, and a full set only retires
    /// projections the same registrant registered - applications sharing an event store never retire each other's.
    /// </param>
    /// <param name="version">
    /// Optional software version of the registering application, as a semantic version. It is stored with each
    /// definition. A definition whose stored version is greater than this one is left out - a registration from an
    /// older client never overwrites a newer one - and a full set that is older than any stored projection of its owner
    /// retires nothing. A missing or unparseable version, on either side, leaves registration unguarded.
    /// </param>
    /// <param name="connectionId">The optional connection declaring these projections, used only for live-client tracking.</param>
    /// <returns>Awaitable task.</returns>
    Task Register(IEnumerable<ProjectionDefinition> definitions, ProjectionOwner? fullSetOwner = null, string? registrant = null, string? version = null, ConnectionId? connectionId = null);

    /// <summary>
    /// Removes an orphaned projection and its observer in every namespace, serialized with registration.
    /// Kernel-owned definitions and projections declared by a connected client are refused.
    /// </summary>
    /// <param name="identifier">The projection identifier.</param>
    /// <returns>The removal result. Projection refusals are store-wide and have no blocking namespace.</returns>
    Task<ObserverRemovalResult> Remove(ProjectionId identifier);

    /// <summary>
    /// Forget a single registered projection.
    /// </summary>
    /// <param name="identifier">The <see cref="ProjectionId"/> of the projection to forget.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Called when an observer is removed outright, so that the manager stops treating the projection as registered.
    /// Without this the definition survives in the manager's own state and the next activation subscribes it again,
    /// recreating the very observer records the removal just deleted. The projection is evicted from the engine and
    /// its own grain state is cleared; its sink container is left untouched, as it is everywhere else.
    /// Forgetting a projection this manager does not know is a no-op, so a removal that partially failed can be
    /// repeated safely.
    /// </remarks>
    Task Forget(ProjectionId identifier);
}
