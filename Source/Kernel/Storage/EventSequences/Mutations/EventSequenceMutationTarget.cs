// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Represents the half-open event sequence range targeted by a mutation.
/// </summary>
/// <param name="Start">The inclusive start sequence number.</param>
/// <param name="EndExclusive">The exclusive end sequence number.</param>
/// <param name="ExpectedCount">The number of events expected in the target range.</param>
public sealed record EventSequenceMutationTarget(EventSequenceNumber Start, EventSequenceNumber EndExclusive, EventCount ExpectedCount);
