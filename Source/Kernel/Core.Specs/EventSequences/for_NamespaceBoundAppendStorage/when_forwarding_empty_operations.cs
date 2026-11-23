// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.EventSequences.for_NamespaceBoundAppendStorage;

public class when_forwarding_empty_operations : given.a_bound_storage
{
    readonly EventToAppendToStorage[] _emptyEvents = [];
    readonly string[] _emptyIdentities = [];
    readonly OccurrenceId[] _emptyOccurrences = [];

    async Task Because()
    {
        await _bound.AppendMany(_emptyEvents);
        await _bound.AppendManyWithIdentities(_emptyEvents);
        await _bound.TryGetPublications(_emptyIdentities);
        await _bound.GetEffectReceiptsForOccurrences(_emptyOccurrences);
        await _bound.AppendManyWithEffectReceipt(_emptyEvents, _receipt with { Outcome = EffectOutcome.NoEvents, EventSequenceId = null, FirstSequenceNumber = null, LastSequenceNumber = null });
    }

    [Fact] async Task should_fence_an_empty_batch() => await _provider.Received(1).AppendMany(_context, _emptyEvents);
    [Fact] async Task should_fence_an_empty_identity_batch() => await _provider.Received(1).AppendManyWithIdentities(_context, _emptyEvents);
    [Fact] async Task should_fence_an_empty_identity_lookup() => await _provider.Received(1).TryGetPublications(_context, _emptyIdentities);
    [Fact] async Task should_fence_an_empty_occurrence_lookup() => await _provider.Received(1).GetEffectReceiptsForOccurrences(_context, _emptyOccurrences);
    [Fact] async Task should_fence_a_receipt_only_commit() => await _provider.Received(1).AppendManyWithEffectReceipt(_context, _emptyEvents, Arg.Is<EffectReceipt>(receipt => receipt.Outcome == EffectOutcome.NoEvents));
}
