// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Persistence.EventSequences;

namespace Aksio.Cratis.Kernel.Persistence;

/// <summary>
/// Defines the storage for a specific instance of an event store.
/// </summary>
public interface IEventStoreInstanceStorage
{
    /// <summary>
    /// Get the <see cref="IEventSequenceStorage"/> for a specific <see cref="EventSequenceId"/>.
    /// </summary>
    /// <param name="eventSequenceId"><see cref="EventSequenceId"/> to get for.</param>
    /// <returns>The <see cref="IEventStoreInstanceStorage"/> instance.</returns>
    IEventSequenceStorage GetEventSequence(EventSequenceId eventSequenceId);
}
