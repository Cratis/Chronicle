// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// The first-writer-wins result of recording an effect without events.
/// </summary>
/// <param name="Status">Whether this call recorded the receipt.</param>
/// <param name="Receipt">The winning receipt, including its stored response.</param>
public record RecordEffectReceiptResult(EffectReceiptStatus Status, EffectReceiptDetails Receipt);
