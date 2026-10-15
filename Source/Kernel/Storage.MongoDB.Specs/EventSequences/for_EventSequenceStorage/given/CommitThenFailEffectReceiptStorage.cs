// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.given;

/// <summary>
/// Performs the real write and then fails every write acknowledgment.
/// </summary>
/// <param name="inner">The real provider.</param>
public class CommitThenFailEffectReceiptStorage(IEffectReceiptStorage inner) : IEffectReceiptStorage
{
    /// <inheritdoc/>
    public Task<EffectReceipt?> GetEffectReceipt(string id) => inner.GetEffectReceipt(id);

    /// <inheritdoc/>
    public async Task<EffectReceiptWriteResult> AppendManyWithEffectReceipt(IEnumerable<EventToAppendToStorage> events, EffectReceipt receipt)
    {
        await inner.AppendManyWithEffectReceipt(events, receipt);
        throw new AcknowledgmentLost();
    }
}
