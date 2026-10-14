// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.given;

public class a_receipt_storage : a_publication_storage
{
    protected readonly string _receiptId = OccurrenceIdentities.ForEffect(new OccurrenceId(Guid.NewGuid().ToString("N")), "effect");

    protected EffectReceipt Receipt(ulong? number) => new(_receiptId, new LeaseToken("occurrence", 1), number.HasValue ? EffectOutcome.Appended : EffectOutcome.NoEvents, number.HasValue ? _eventSequenceIdForReceipt : null, number.HasValue ? new EventSequenceNumber(number.Value) : null, number.HasValue ? new EventSequenceNumber(number.Value) : null, [], "{\"winner\":1}", DateTimeOffset.UnixEpoch);

    Cratis.Chronicle.Concepts.EventSequences.EventSequenceId _eventSequenceIdForReceipt => Cratis.Chronicle.Concepts.EventSequences.EventSequenceId.Outbox;
    protected IEffectReceiptStorage Recreate() => _storage;
    protected EventToAppendToStorage EventAt(EventSequenceNumber number) => _event with { SequenceNumber = number };
}
