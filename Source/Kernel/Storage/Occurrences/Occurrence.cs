// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Occurrences;

/// <summary>
/// The durable ingress and delivery facts for an occurrence.
/// </summary>
/// <param name="Id">The deduplication identity.</param>
/// <param name="Trigger">The trigger identity.</param>
/// <param name="Kind">The source kind.</param>
/// <param name="Fingerprint">The immutable ingress fingerprint.</param>
/// <param name="Payload">The validated JSON payload.</param>
/// <param name="SchemaRevision">The schema revision.</param>
/// <param name="RecordedAt">The kernel recording instant.</param>
/// <param name="Due">The schedule due instant, if any.</param>
/// <param name="DefinitionRevision">The materializing definition revision.</param>
/// <param name="State">The delivery state.</param>
/// <param name="Attempts">The attempts made.</param>
/// <param name="Epoch">The fencing epoch.</param>
/// <param name="Owner">The current worker.</param>
/// <param name="LeaseExpiry">The kernel lease expiry.</param>
/// <param name="LastError">The most recent error.</param>
/// <param name="NextAttempt">The retry instant.</param>
public record Occurrence(
    OccurrenceId Id,
    TriggerId Trigger,
    OccurrenceKind Kind,
    string Fingerprint,
    string Payload,
    long SchemaRevision,
    DateTimeOffset RecordedAt,
    DateTimeOffset? Due,
    long? DefinitionRevision,
    OccurrenceState State,
    int Attempts,
    long Epoch,
    string? Owner,
    DateTimeOffset? LeaseExpiry,
    string? LastError,
    DateTimeOffset? NextAttempt);
