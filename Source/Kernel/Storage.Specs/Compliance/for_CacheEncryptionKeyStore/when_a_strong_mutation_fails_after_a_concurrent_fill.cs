// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore;

public class when_a_strong_mutation_fails_after_a_concurrent_fill : given.a_cache_encryption_key_store
{
    static readonly EventStoreName _eventStore = "store";
    static readonly EventStoreNamespaceName _namespace = "namespace";
    static readonly EncryptionKeyIdentifier _identifier = "subject";
    Exception? _error;

    async Task Establish()
    {
        _actualStore.TryGetFor(_eventStore, _namespace, _identifier).Returns(new EncryptionKey([1], [2]));
        await _store.GetFor(_eventStore, _namespace, _identifier);
        _actualStore.ConfirmErasureFor(_eventStore, _namespace, _identifier, "request").Returns(async _ =>
        {
            await _store.GetFor(_eventStore, _namespace, _identifier);
            throw new EncryptionKeyFenceUnavailable(_identifier);
        });
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _store.ConfirmErasureFor(_eventStore, _namespace, _identifier, "request"));
        await _store.GetFor(_eventStore, _namespace, _identifier);
    }

    [Fact] void should_propagate_the_failure() => _error.ShouldBeOfExactType<EncryptionKeyFenceUnavailable>();
    [Fact] void should_invalidate_both_before_and_after_the_failure() => _actualStore.Received(3).TryGetFor(_eventStore, _namespace, _identifier);
}
