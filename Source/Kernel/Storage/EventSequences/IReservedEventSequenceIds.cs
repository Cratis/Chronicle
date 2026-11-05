// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// Defines the event sequence identifiers the active storage provider keeps for its own data.
/// </summary>
public interface IReservedEventSequenceIds
{
    /// <summary>
    /// Determines whether an event sequence with the given identifier would share storage with internal data.
    /// </summary>
    /// <param name="eventSequenceId">The <see cref="EventSequenceId"/> to check.</param>
    /// <returns>True when the identifier equals (ignoring case) a collection or table name the provider keeps for itself, or ends with a suffix the provider derives per event sequence or read model; otherwise false.</returns>
    bool IsReserved(EventSequenceId eventSequenceId);
}
