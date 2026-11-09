// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager.when_deleting_an_encryption_key;

public class and_a_fence_write_outcome_is_unknown : given.a_pii_manager
{
    readonly List<string> _requestIds = [];
    Exception _error;

    void Establish() =>
        _keyStore
            .RecordErasureFor(EventStore, EventStoreNamespace, Identifier, Arg.Any<EncryptionKeyErasureRequest>())
            .Returns(
                call =>
                {
                    _requestIds.Add(call.Arg<EncryptionKeyErasureRequest>().RequestId);
                    return _requestIds.Count == 1
                        ? Task.FromException(new EncryptionKeyFenceWriteOutcomeUnknown(Identifier, new TimeoutException()))
                        : Task.CompletedTask;
                });

    async Task Because() => _error = await Catch.Exception(() => _manager.DeleteEncryptionKeyFor(Identifier));

    [Fact] void should_retry_the_write_once() => _requestIds.Count.ShouldEqual(2);
    [Fact] void should_retry_with_the_same_request_id() => _requestIds.Distinct().Count().ShouldEqual(1);
    [Fact] void should_complete_the_erasure() => _error.ShouldBeNull();
}
