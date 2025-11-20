// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding;

/// <summary>
/// Defines a system that can seed events into the event store.
/// </summary>
public interface ICanSeedEvents
{
    /// <summary>
    /// Seed events into the event store.
    /// </summary>
    /// <param name="builder">The <see cref="IEventSeedingBuilder"/> to use.</param>
    void Seed(IEventSeedingBuilder builder);
}
