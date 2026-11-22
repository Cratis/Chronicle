// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Identifies the Chronicle effect of an occurrence and its optional opaque JSON response.
/// </summary>
/// <param name="OccurrenceId">The stable occurrence identity.</param>
/// <param name="EffectKey">The nonempty effect name chosen by the handler.</param>
/// <param name="Epoch">The lease epoch that executed the effect.</param>
/// <param name="Response">The verbatim JSON response, at most 64 KiB UTF-8; never personal data.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Occurrences.EffectReceipt")]
public record EffectReceipt([Id(0)] string OccurrenceId, [Id(1)] string EffectKey, [Id(2)] long Epoch, [Id(3)] string? Response = null);
