// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// The immutable winning receipt returned by an effect commit.
/// </summary>
/// <param name="Id">The namespace-scoped receipt identity.</param>
/// <param name="OccurrenceId">The occurrence identity.</param>
/// <param name="Epoch">The epoch that wrote the receipt.</param>
/// <param name="Outcome">Appended, NoEvents or Refused.</param>
/// <param name="EventSequenceId">The appended sequence, if any.</param>
/// <param name="FirstSequenceNumber">The first appended slot, if any.</param>
/// <param name="LastSequenceNumber">The last appended slot, if any.</param>
/// <param name="Reasons">The refusal reasons.</param>
/// <param name="Response">The optional verbatim JSON response.</param>
/// <param name="RecordedAt">The kernel's recording instant.</param>
public record EffectReceiptDetails(
    string Id,
    string OccurrenceId,
    long Epoch,
    EffectOutcome Outcome,
    string? EventSequenceId,
    ulong? FirstSequenceNumber,
    ulong? LastSequenceNumber,
    IEnumerable<string> Reasons,
    string? Response,
    DateTimeOffset RecordedAt);
