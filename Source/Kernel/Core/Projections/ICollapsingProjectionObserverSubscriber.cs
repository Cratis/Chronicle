// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines a specialized <see cref="IObserverSubscriber"/> for projections whose key can collapse several event
/// sources onto one read model document - a constant key, a key read from event content, joins, or a parent
/// hierarchy.
/// </summary>
/// <remarks>
/// Handling such a projection is a read-modify-write cycle against a document several partitions share, serialized
/// by the coarse mode of
/// <see cref="Cratis.Chronicle.Projections.Engine.Pipelines.ProjectionHandleLock"/>. That lock lives in the
/// process, so the partitions must not be spread across silos: being
/// <see cref="IUnpartitionedObserverSubscriber">unpartitioned</see> collapses them onto one activation, which
/// Orleans keeps single cluster wide. The projection therefore does not scale out across silos - a deliberate
/// trade, and not a new limit, since the coarse lock already serialized the whole projection within a silo.
/// </remarks>
public interface ICollapsingProjectionObserverSubscriber : IObserverSubscriber, IAmOwnedByKernel, IUnpartitionedObserverSubscriber;
