// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Orleans.Services;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines a service that lives in each silo and tells that silo's observer definition registry that the definition of
/// an observer changed.
/// </summary>
public interface IObserverDefinitionsCacheGrainService : IGrainService
{
    /// <summary>
    /// Make the local silo's observer definition registry read the definition of an observer again.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the observer belongs to.</param>
    /// <param name="observerId">The <see cref="ObserverId"/> whose definition changed or was deleted.</param>
    /// <returns>Awaitable task.</returns>
    Task Invalidate(EventStoreName eventStore, ObserverId observerId);
}
