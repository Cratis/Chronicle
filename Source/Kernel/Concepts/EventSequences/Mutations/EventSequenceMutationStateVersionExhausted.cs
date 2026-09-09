// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.EventSequences.Mutations;

/// <summary>
/// The exception that is thrown when an event sequence mutation state version cannot be incremented.
/// </summary>
/// <param name="version">The exhausted state version.</param>
public class EventSequenceMutationStateVersionExhausted(EventSequenceMutationStateVersion version) :
    Exception($"The event sequence mutation state version '{version.Value}' cannot be incremented because its range is exhausted.");
