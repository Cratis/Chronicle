// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceSnapshot.given;

public class an_application_snapshot : a_durable_snapshot
{
    protected readonly EventStoreNamespaceName _otherNamespace = new("other-tenant");
    protected readonly IEventSequenceStorage _otherSequence = Substitute.For<IEventSequenceStorage, IEffectReceiptStorage>();
    protected Occurrence _secondOccurrence;

    void Establish()
    {
        var otherTenant = Substitute.For<IEventStoreNamespaceStorage>();
        _store.Namespaces.GetAll().Returns(Task.FromResult<IEnumerable<NamespaceState>>([
            new(_namespace, DateTimeOffset.UnixEpoch), new(_otherNamespace, DateTimeOffset.UnixEpoch)]));
        _store.GetNamespace(_otherNamespace).Returns(otherTenant);
        otherTenant.GetEventSequence(EventSequenceId.Log).Returns(_otherSequence);
        _secondOccurrence = _occurrence with { Id = new("second-occurrence") };
        _occurrences.GetForTrigger(_occurrence.Trigger).Returns(Task.FromResult<IEnumerable<Occurrence>>([_occurrence, _secondOccurrence]));
        ((IEffectReceiptStorage)_otherSequence).GetEffectReceiptsForOccurrences(Arg.Any<IEnumerable<OccurrenceId>>()).Returns(Task.FromResult<IEnumerable<Storage.Occurrences.EffectReceipt>>([
            new("receipt", new(_occurrence.Id, 6), EffectOutcome.NoEvents, null, null, null, [], "{\"answer\":99}", DateTimeOffset.UnixEpoch),
            new("second-receipt", new(_secondOccurrence.Id, 7), EffectOutcome.Refused, null, null, null, ["refused"], null, DateTimeOffset.UnixEpoch)]));
    }
}
