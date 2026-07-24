// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;
extern alias KernelCore;

using KernelCore::Cratis.Chronicle.EventTypes;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents a no-op <see cref="IEventTypesChangedNotifier"/> for the single-process test harness, where
/// there are no peer silos to notify.
/// </summary>
internal sealed class NoOpEventTypesChangedNotifier : IEventTypesChangedNotifier
{
    /// <inheritdoc/>
    public Task Notify(
        KernelConcepts::Cratis.Chronicle.Concepts.EventStoreName eventStore,
        KernelConcepts::Cratis.Chronicle.Concepts.Events.EventTypeId eventTypeId) => Task.CompletedTask;
}
