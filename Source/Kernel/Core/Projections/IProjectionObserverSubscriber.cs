// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines a specialized <see cref="IObserverSubscriber"/> for projections whose read model key always resolves to
/// the event's own event source id.
/// </summary>
/// <remarks>
/// Every event source targets its own read model document, so a subscriber activation per partition is safe and
/// the activations spread across the silos of a cluster. A projection whose key can collapse several event sources
/// onto one document subscribes as <see cref="ICollapsingProjectionObserverSubscriber"/> instead.
/// </remarks>
public interface IProjectionObserverSubscriber : IObserverSubscriber, IAmOwnedByKernel;
