// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint.when_comparing_after_the_key_changed;

public class and_the_key_storage_is_unavailable : given.a_stored_protected_intent
{
    EncryptionKeyStorageUnavailable _failure;
    Exception _error;
    void Establish()
    {
        var unavailable = Substitute.For<IEncryptionKeyStorage>();
        _failure = new("subject", []);
        unavailable.TryGetFor("store", "ns", "subject", _original.ProtectedParts[0].Revision).Returns(Task.FromException<EncryptionKey?>(_failure));
        _cache = new(unavailable);
    }
    async Task Because() => _error = await Catch.Exception(Compare);
    [Fact] void should_propagate_the_storage_failure() => _error.ShouldBeSame(_failure);
}
