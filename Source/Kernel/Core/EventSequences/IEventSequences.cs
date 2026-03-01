// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Defines a system that manages all the event sequences for an event store and namespace.
/// </summary>
public interface IEventSequences : IGrainWithIntegerCompoundKey
{
    /// <summary>
    /// Rehydrate the event sequences.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Rehydrate();
}
