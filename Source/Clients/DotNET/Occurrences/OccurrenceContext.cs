// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Describes one delivery attempt. Due and recorded times come from the kernel clock, not the execution start.
/// </summary>
/// <param name="Id">The stable occurrence identity.</param>
/// <param name="Trigger">The registered trigger.</param>
/// <param name="Kind">The occurrence source.</param>
/// <param name="DueAt">The due instant for a scheduled occurrence.</param>
/// <param name="RecordedAt">The durable ingress instant.</param>
/// <param name="Revision">The schedule definition revision, when scheduled.</param>
/// <param name="Attempt">The delivery attempt number.</param>
/// <param name="Lease">The current fenced lease.</param>
public record OccurrenceContext(
    OccurrenceId Id,
    TriggerId Trigger,
    OccurrenceKind Kind,
    DateTimeOffset? DueAt,
    DateTimeOffset RecordedAt,
    ScheduleRevision? Revision,
    int Attempt,
    LeaseToken Lease)
{
    /// <summary>
    /// Identifies an effect across redelivery. Pass its Value to external idempotent endpoints.
    /// </summary>
    /// <param name="name">The stable name of the effect within this occurrence.</param>
    /// <returns>The attempt-independent effect identity with the current writing lease.</returns>
    public EffectKey EffectKey(EffectName name) => new(Id, name, Lease);
}
