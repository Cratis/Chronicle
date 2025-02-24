// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents a failed partition attempt.
/// </summary>
/// <param name="Occurred">When it occurred.</param>
/// <param name="SequenceNumber">Event sequence number it occured at.</param>
/// <param name="Messages">Collection of messages associated.</param>
/// <param name="StackTrace">Associated stack trace.</param>
public record FailedPartitionAttempt(DateTimeOffset Occurred, EventSequenceNumber SequenceNumber, IEnumerable<string> Messages, string StackTrace);
