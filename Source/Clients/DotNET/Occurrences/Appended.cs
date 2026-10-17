// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// An effect that appended events atomically with its receipt.
/// </summary>
/// <param name="EventSequenceId">The event sequence.</param>
/// <param name="First">The first appended slot.</param>
/// <param name="Last">The last appended slot.</param>
public record Appended(EventSequenceId EventSequenceId, EventSequenceNumber First, EventSequenceNumber Last) : EffectOutcome;
