// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// The durable winning receipt and whether this attempt wrote it.
/// </summary>
/// <param name="Status">Whether the receipt was newly recorded.</param>
/// <param name="Receipt">The winning receipt, including its stored response.</param>
public record EffectReceiptResult(EffectReceiptStatus Status, EffectReceipt Receipt);
