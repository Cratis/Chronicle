// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents a partition a batch method reported it could not handle.
/// </summary>
/// <param name="Partition">The <see cref="EventSourceId"/> of the partition.</param>
/// <param name="At">The sequence number of the first event not handled.</param>
/// <param name="Kind">The <see cref="BatchOutcomeKind"/>.</param>
/// <param name="Exception">The <see cref="System.Exception"/> describing a failure, if any.</param>
/// <param name="RetryAfter">How long to wait before delivering the partition again, for a deferral.</param>
internal sealed record BatchOutcome(EventSourceId Partition, EventSequenceNumber At, BatchOutcomeKind Kind, Exception? Exception, TimeSpan RetryAfter);
