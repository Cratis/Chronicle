// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// The exception that is thrown when routing needs a reserved scope label that is already occupied.
/// </summary>
/// <param name="label">The occupied label.</param>
public class ReservedConcurrencyScopeLabelCollision(EventSourceId label)
    : Exception($"Reserved concurrency scope label '{label}' is already occupied.");
