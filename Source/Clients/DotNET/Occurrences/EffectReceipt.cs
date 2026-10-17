// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// The first durable outcome of an effect. Response is plaintext and must not contain personal data.
/// </summary>
/// <param name="Key">The effect identity.</param>
/// <param name="Outcome">The recorded outcome.</param>
/// <param name="Response">The optional opaque JSON response, at most 64 KiB.</param>
/// <param name="WrittenBy">The lease that won the write.</param>
/// <param name="RecordedAt">The kernel recording instant.</param>
public record EffectReceipt(EffectKey Key, EffectOutcome Outcome, JsonNode? Response, LeaseToken WrittenBy, DateTimeOffset RecordedAt);
