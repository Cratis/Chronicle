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
public record EffectReceipt(string OccurrenceId, string EffectKey, long Epoch, string? Response = null);
