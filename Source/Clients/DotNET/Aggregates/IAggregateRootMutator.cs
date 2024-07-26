// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Defines a system that can mutate an <see cref="IAggregateRoot"/>.
/// </summary>
public interface IAggregateRootMutator
{
    /// <summary>
    /// Rehydrate the mutation.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Rehydrate();

    /// <summary>
    /// Mutate the aggregate root with an event.
    /// </summary>
    /// <param name="event">Event to mutate with.</param>
    /// <returns>Awaitable task.</returns>
    Task Mutate(object @event);

    /// <summary>
    /// Dehydrate the mutation.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Dehydrate();
}
