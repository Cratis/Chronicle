// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Identifies the namespace containing an occurrence's winning effect receipt.
/// </summary>
/// <param name="Namespace">The namespace in which the effect settled.</param>
/// <param name="Receipt">The immutable recorded effect.</param>
public record OccurrenceEffectReceipt(string Namespace, EffectReceiptDetails Receipt);
