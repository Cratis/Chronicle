// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.EventSequences.for_NamespaceBoundAppendStorage.given;

public class a_bound_storage : Specification
{
    protected INamespaceFencedEventSequenceStorage _provider;
    protected NamespaceCommitContext _context;
    protected NamespaceBoundAppendStorage _bound;
    protected EventToAppendToStorage _event;
    protected EventToAppendToStorage[] _events;
    protected readonly EventPublication _publication = new("publication", "fingerprint");
    protected readonly string[] _identities = ["publication"];
    protected readonly OccurrenceId _occurrence = "occurrence";
    protected readonly OccurrenceId[] _occurrences = ["occurrence"];
    protected EffectReceipt _receipt;

    void Establish()
    {
        _provider = Substitute.For<INamespaceFencedEventSequenceStorage>();
        var incarnation = NamespaceIncarnationId.New();
        _context = new(incarnation, CancellationToken.None) { ActivationIncarnation = incarnation };
        _bound = new(_provider, _context);
        _event = new(0, EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, new EventType("type", 1), CorrelationId.New(), [], [], [], DateTimeOffset.UnixEpoch, new ExpandoObject(), EventHash.NotSet)
        {
            NamedTags = [new("region", "east")],
            CompensationFor = 3,
            Identity = _publication
        };
        _events = [_event];
        _receipt = new("receipt", new LeaseToken(_occurrence, 1), EffectOutcome.Appended, "sequence", 0, 0, [], "{}", DateTimeOffset.UnixEpoch);
    }

    protected Func<Task>[] Operations() =>
    [
        () => _bound.Append(_event),
        () => _bound.AppendMany(_events),
        () => _bound.AppendManyWithIdentities(_events),
        () => _bound.TryGetPublication(_publication),
        () => _bound.TryGetPublications(_identities),
        () => _bound.AppendPublication(_publication, _event),
        () => _bound.GetEffectReceipt(_receipt.Id),
        () => _bound.GetEffectReceipts(_occurrence),
        () => _bound.GetEffectReceiptsForOccurrences(_occurrences),
        () => _bound.AppendManyWithEffectReceipt(_events, _receipt)
    ];
}
