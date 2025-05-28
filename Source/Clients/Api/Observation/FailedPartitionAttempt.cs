// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Observation;

/// <summary>
/// Represents the state of an attempt of a failed partition. This includes representing the initial attempt that caused the failure.
/// </summary>
/// <param name="Occurred">When the attempt occurred.</param>
/// <param name="SequenceNumber">The sequence number for the event that caused the failure.</param>
/// <param name="Messages">The error messages for the last error on this failed partition.</param>
/// <param name="StackTrace">The stack trace for the last error on this failed partition.</param>
public record FailedPartitionAttempt(DateTimeOffset Occurred, ulong SequenceNumber, IEnumerable<string> Messages, string StackTrace);
