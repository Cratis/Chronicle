// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
namespace Cratis.Chronicle.Grains.Observation;

public static class FailedPartitionsHelpers
{
    public static FailedPartition AddFailedPartition(
        this FailedPartitions failedPartitions,
        EventSourceId eventSourceId,
        EventSequenceNumber? eventSequenceNumber = default,
        IEnumerable<string>? messages = default,
        string stackTrace = "") =>
        failedPartitions.RegisterAttempt(eventSourceId.Value, eventSequenceNumber ?? EventSequenceNumber.First, messages ?? [], stackTrace);
}
