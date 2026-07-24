// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;
extern alias KernelCore;

using KernelCore::Cratis.Chronicle.EventTypes;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents a no-op <see cref="IEventTypesCacheClient"/> for the single-process test harness, where there
/// are no peer silos to invalidate.
/// </summary>
internal sealed class NoOpEventTypesCacheClient : IEventTypesCacheClient
{
    /// <inheritdoc/>
    public Task Invalidate(
        KernelConcepts::Cratis.Chronicle.Concepts.EventStoreName eventStore,
        KernelConcepts::Cratis.Chronicle.Concepts.Events.EventTypeId eventTypeId) => Task.CompletedTask;
}
