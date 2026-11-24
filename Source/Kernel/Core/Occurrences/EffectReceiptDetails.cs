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
[GenerateSerializer]
[Alias("Cratis.Chronicle.Occurrences.EffectReceiptDetails")]
public record EffectReceiptDetails(
    [Id(0)] string Id,
    [Id(1)] string OccurrenceId,
    [Id(2)] long Epoch,
    [Id(3)] EffectOutcome Outcome,
    [Id(4)] string? EventSequenceId,
    [Id(5)] ulong? FirstSequenceNumber,
    [Id(6)] ulong? LastSequenceNumber,
    [Id(7)] IEnumerable<string> Reasons,
    [Id(8)] string? Response,
    [Id(9)] DateTimeOffset RecordedAt);
