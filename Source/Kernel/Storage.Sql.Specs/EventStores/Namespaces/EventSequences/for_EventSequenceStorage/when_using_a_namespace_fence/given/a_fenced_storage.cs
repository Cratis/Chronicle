// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_using_a_namespace_fence.given;

public class a_fenced_storage : for_EventSequenceStorage.given.a_receipt_storage
{
    protected readonly NamespaceIncarnationId _incarnation = NamespaceIncarnationId.New();
    protected NamespaceCommitContext _context;
    protected NamespaceMarkerStorage _markers;
    protected INamespaceFencedEventSequenceStorage _fenced;

    async Task Establish()
    {
        _markers = new(() => _database.Namespace(_eventStore, _namespace));
        (await _markers.TryOpen(_incarnation, null)).ShouldBeTrue();
        _context = new(_incarnation, CancellationToken.None) { ActivationIncarnation = _incarnation };
        _fenced = _storage;
        _event = _event with { NamedTags = [new("region", "east")], CompensationFor = 3, Identity = _publication };
    }

    protected Func<Task>[] FencedOperations() =>
    [
        () => _fenced.Append(_context, _event),
        () => _fenced.AppendMany(_context, [_event]),
        () => _fenced.AppendMany(_context, []),
        () => _fenced.AppendManyWithIdentities(_context, [_event]),
        () => _fenced.AppendPublication(_context, _publication, _event),
        () => _fenced.TryGetPublication(_context, _publication),
        () => _fenced.TryGetPublications(_context, []),
        () => _fenced.GetEffectReceipt(_context, _receiptId),
        () => _fenced.GetEffectReceipts(_context, Receipt(null).Lease.OccurrenceId),
        () => _fenced.GetEffectReceiptsForOccurrences(_context, []),
        () => _fenced.AppendManyWithEffectReceipt(_context, [], Receipt(null))
    ];
}
