// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceSnapshot.given;

public class a_durable_snapshot : Specification
{
    protected readonly EventStoreName _storeName = new("store");
    protected readonly EventStoreNamespaceName _namespace = new("tenant");
    protected readonly IStorage _storage = Substitute.For<IStorage>();
    protected readonly IEventStoreStorage _store = Substitute.For<IEventStoreStorage>();
    protected readonly IEventStoreNamespaceStorage _tenant = Substitute.For<IEventStoreNamespaceStorage>();
    protected readonly IOccurrencesStorage _occurrences = Substitute.For<IOccurrencesStorage>();
    protected readonly IEventSequenceStorage _sequence = Substitute.For<IEventSequenceStorage, IEffectReceiptStorage>();
    protected Occurrence _occurrence;

    void Establish()
    {
        _storage.GetEventStore(_storeName).Returns(_store);
        _store.GetNamespace(_namespace).Returns(_tenant);
        _store.Occurrences.Returns(_occurrences);
        _tenant.Occurrences.Returns(_occurrences);
        _tenant.GetEventSequence(EventSequenceId.Log).Returns(_sequence);
        _occurrence = new(new("occurrence"), new("trigger"), OccurrenceKind.Application, "fingerprint", "{}", 3, DateTimeOffset.UnixEpoch, null, null, OccurrenceState.Leased, 2, 7, "worker", DateTimeOffset.UnixEpoch.AddMinutes(1), "previous failure", null);
        _occurrences.Get(_occurrence.Id).Returns(_occurrence);
        _occurrences.GetForTrigger(_occurrence.Trigger).Returns(Task.FromResult<IEnumerable<Occurrence>>([_occurrence]));
        ((IEffectReceiptStorage)_sequence).GetEffectReceiptsForOccurrences(Arg.Any<IEnumerable<OccurrenceId>>()).Returns(Task.FromResult<IEnumerable<Storage.Occurrences.EffectReceipt>>([
            new("receipt", new(_occurrence.Id, 6), EffectOutcome.NoEvents, null, null, null, [], "{\"answer\":42}", DateTimeOffset.UnixEpoch)]));
    }
}
