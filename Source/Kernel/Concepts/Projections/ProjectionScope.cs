// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections;

/// <summary>
/// Represents the scope a projection materializes its read model in.
/// </summary>
/// <remarks>
/// <para>
/// A namespace is the tenant boundary, so by default a projection materializes once per namespace and sees only
/// the events appended there. Some read models - the operational figures the Workbench shows for a whole server -
/// need the same shape aggregated across every namespace in the event store instead.
/// </para>
/// <para>
/// The global scope follows the convention event seeding established: a globally scoped instance is keyed with
/// <see cref="EventStoreNamespaceName.NotSet"/> and stored at event store level, which is what
/// EventSeedingKey.IsGlobal already means. Nothing else about the projection changes - the same definition
/// produces both, which is the point of <see cref="ProjectionScope.Both"/>.
/// </para>
/// </remarks>
public enum ProjectionScope
{
    /// <summary>
    /// The projection materializes once per namespace, seeing only that namespace's events.
    /// </summary>
    Namespaced = 0,

    /// <summary>
    /// The projection materializes once for the event store, aggregating every namespace's events.
    /// </summary>
    Global = 1,

    /// <summary>
    /// The projection materializes both per namespace and once for the event store.
    /// </summary>
    /// <remarks>
    /// This is what makes a figure actionable: the global instance says a number is wrong, and the namespaced
    /// instances say which namespace it is wrong in.
    /// </remarks>
    Both = 2
}
