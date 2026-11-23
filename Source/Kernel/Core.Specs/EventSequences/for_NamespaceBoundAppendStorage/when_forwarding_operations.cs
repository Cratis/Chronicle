// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_NamespaceBoundAppendStorage;

public class when_forwarding_operations : given.a_bound_storage
{
    async Task Because()
    {
        foreach (var operation in Operations()) await operation();
    }

    [Fact] async Task should_forward_single_append() => await _provider.Received(1).Append(_context, _event);
    [Fact] async Task should_forward_batch_with_named_tags() => await _provider.Received(1).AppendMany(_context, _events);
    [Fact] async Task should_forward_identity_batch() => await _provider.Received(1).AppendManyWithIdentities(_context, _events);
    [Fact] async Task should_forward_publication_lookup() => await _provider.Received(1).TryGetPublication(_context, _publication);
    [Fact] async Task should_forward_identity_lookup() => await _provider.Received(1).TryGetPublications(_context, _identities);
    [Fact] async Task should_forward_publication_append() => await _provider.Received(1).AppendPublication(_context, _publication, _event);
    [Fact] async Task should_forward_receipt_lookup() => await _provider.Received(1).GetEffectReceipt(_context, _receipt.Id);
    [Fact] async Task should_forward_occurrence_lookup() => await _provider.Received(1).GetEffectReceipts(_context, _occurrence);
    [Fact] async Task should_forward_occurrences_lookup() => await _provider.Received(1).GetEffectReceiptsForOccurrences(_context, _occurrences);
    [Fact] async Task should_forward_receipt_commit() => await _provider.Received(1).AppendManyWithEffectReceipt(_context, _events, _receipt);
    [Fact] void should_only_use_the_bound_context() => _provider.ReceivedCalls().ShouldEachConformTo(call => ReferenceEquals(call.GetArguments()[0], _context));
}
