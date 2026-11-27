// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_using_namespace_receipts;

public class and_effect_commit_is_unknown : given.a_fenced_receipt_storage
{
    void Establish()
    {
        _commitFailure = new InvalidOperationException("response lost");
        _commitFailures = 1;
    }

    async Task Because() => _failure = await Catch.Exception(() => _storage.AppendManyWithEffectReceipt(_context, [_event], _receipt));

    [Fact] void should_preserve_unknown_commit() => _failure.ShouldBeOfExactType<NamespaceCommitOutcomeUnknown>();
    [Fact] void should_not_retry() => _attempts.ShouldEqual(1);
    [Fact] void should_not_lookup_receipts_after_unknown_commit() => _receiptReadSessions.Count.ShouldEqual(1);
}
