// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Orleans.Concurrency;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Defines the constraints system for an event store.
/// </summary>
public interface IConstraints : IGrainWithStringKey
{
    /// <summary>
    /// Register a set of definitions.
    /// </summary>
    /// <param name="definitions">Collection of <see cref="IConstraintDefinition"/> to register.</param>
    /// <returns>Awaitable task.</returns>
    Task Register(IEnumerable<IConstraintDefinition> definitions);

    /// <summary>
    /// Get the currently registered constraint definitions.
    /// </summary>
    /// <returns>A snapshot of the registered <see cref="IConstraintDefinition"/> for the event store.</returns>
    /// <remarks>
    /// Served from the grain's in-memory state so that callers can resolve the current definitions without querying
    /// storage. The snapshot is re-read from persisted state on activation.
    /// <para>
    /// Interleaved with a registration in progress, which serves the definitions as they were last persisted.
    /// </para>
    /// </remarks>
    [AlwaysInterleave]
    Task<IReadOnlyCollection<IConstraintDefinition>> GetDefinitions();

    /// <summary>
    /// Get the current <see cref="ConstraintsVersion"/> for the event store.
    /// </summary>
    /// <returns>A content-derived stamp that changes whenever the registered definitions change.</returns>
    /// <remarks>
    /// A cheap, cluster-safe signal callers cache and compare on each append to detect that constraints have changed
    /// since they last read them. It is derived from the content of the definitions, so it is stable across grain
    /// deactivation and identical across silos for identical definitions.
    /// <para>
    /// Interleaved with a registration in progress, so an append checking it is never queued behind one - and a
    /// registration refreshing an event sequence cannot deadlock with that sequence's append. A new version is only
    /// served once the definitions behind it are persisted.
    /// </para>
    /// </remarks>
    [AlwaysInterleave]
    Task<ConstraintsVersion> GetVersion();
}
