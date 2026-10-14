// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Occurrences;

/// <summary>
/// An immutable, first-writer-wins effect receipt. Response is verbatim JSON, without personal data.
/// </summary>
/// <param name="Id">The effect identity hash prefixed with effect:.</param>
/// <param name="Lease">The token that wrote this receipt.</param>
/// <param name="Outcome">The recorded outcome.</param>
/// <param name="EventSequenceId">The appended sequence, if any.</param>
/// <param name="FirstSequenceNumber">The first appended slot, if any.</param>
/// <param name="LastSequenceNumber">The last appended slot, if any.</param>
/// <param name="Reasons">The refusal reasons.</param>
/// <param name="Response">The opaque JSON response, at most 64 KiB UTF-8.</param>
/// <param name="RecordedAt">The recording instant.</param>
public record EffectReceipt(
    string Id,
    LeaseToken Lease,
    EffectOutcome Outcome,
    Cratis.Chronicle.Concepts.EventSequences.EventSequenceId? EventSequenceId,
    Cratis.Chronicle.Concepts.Events.EventSequenceNumber? FirstSequenceNumber,
    Cratis.Chronicle.Concepts.Events.EventSequenceNumber? LastSequenceNumber,
    IReadOnlyList<string> Reasons,
    string? Response,
    DateTimeOffset RecordedAt);
