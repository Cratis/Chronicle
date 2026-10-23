// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Represents a timeout that ended a job step, kept so the partition can be reported as failed if the step runs out of attempts.
/// </summary>
/// <param name="Partition">The partition the step was handling.</param>
/// <param name="FailedAt">The sequence number of the event the step was handling.</param>
/// <param name="Messages">The short description of the timeout.</param>
public record TransientPartitionFailure(Key Partition, EventSequenceNumber FailedAt, IList<string> Messages)
{
    /// <summary>
    /// Gets the optional original delivery identity and deadline.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DeliveryFenceContext? DeliveryFence { get; init; }
}
