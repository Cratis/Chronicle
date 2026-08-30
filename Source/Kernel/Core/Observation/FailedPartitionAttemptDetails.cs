// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents one attempt at handling the event that failed a partition.
/// </summary>
/// <param name="Occurred">When the attempt happened.</param>
/// <param name="SequenceNumber">The sequence number of the event the attempt handled.</param>
/// <param name="Messages">The messages describing why the attempt failed.</param>
/// <param name="StackTrace">The stack trace of the failure.</param>
/// <param name="Kind">What kind of thing went wrong on the attempt.</param>
public record FailedPartitionAttemptDetails(
    DateTimeOffset Occurred,
    ulong SequenceNumber,
    IEnumerable<string> Messages,
    string StackTrace,
    FailureKind Kind = FailureKind.Unknown);
