// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// Represents a partition a reactor batch method reported as failed or deferred.
/// </summary>
/// <param name="Partition">The <see cref="EventSourceId"/> of the partition.</param>
/// <param name="At">The <see cref="EventSequenceNumber"/> the partition fails or is delivered again from.</param>
/// <param name="Kind">The <see cref="BatchPartitionOutcomeKind"/>.</param>
/// <param name="Exception">The <see cref="System.Exception"/> a failure was reported with, or null for a deferral.</param>
/// <param name="RetryAfter">How long a deferred partition waits before it is delivered again.</param>
public record BatchPartitionOutcome(
    EventSourceId Partition,
    EventSequenceNumber At,
    BatchPartitionOutcomeKind Kind,
    Exception? Exception,
    TimeSpan RetryAfter);
