// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Represents a failed partition in a bulk operation.
/// </summary>
/// <param name="EventSourceId">The event source identifier for the failed partition.</param>
/// <param name="EventSequenceNumber">The sequence number of the first event that failed.</param>
public record FailedPartition(Key EventSourceId, EventSequenceNumber EventSequenceNumber);
