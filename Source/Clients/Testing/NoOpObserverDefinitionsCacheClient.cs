// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;
extern alias KernelCore;

using KernelCore::Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Represents a no-op <see cref="IObserverDefinitionsCacheClient"/> for the single-process test harness, where there
/// are no peer silos whose observer definition registry needs refreshing.
/// </summary>
internal sealed class NoOpObserverDefinitionsCacheClient : IObserverDefinitionsCacheClient
{
    /// <inheritdoc/>
    public Task Invalidate(
        KernelConcepts::Cratis.Chronicle.Concepts.EventStoreName eventStore,
        KernelConcepts::Cratis.Chronicle.Concepts.Observation.ObserverId observerId) => Task.CompletedTask;
}
