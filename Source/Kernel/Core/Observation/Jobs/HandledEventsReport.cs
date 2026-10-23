// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Represents the handled-event counts a job step reports to its observer for one handled batch.
/// </summary>
/// <param name="Partition">The partition the counts are for.</param>
/// <param name="Counts">The number of handled events per event type.</param>
/// <param name="ReportedBy">The <see cref="JobStepId"/> of the reporting step, which with <paramref name="HandledUpTo"/> identifies the report.</param>
/// <param name="HandledUpTo">The <see cref="EventSequenceNumber"/> of the last event the counts cover.</param>
[GenerateSerializer]
public sealed record HandledEventsReport(
    [property: Id(0)] Key Partition,
    [property: Id(1)] IReadOnlyDictionary<EventTypeId, EventCount> Counts,
    [property: Id(2)] JobStepId ReportedBy,
    [property: Id(3)] EventSequenceNumber HandledUpTo);
