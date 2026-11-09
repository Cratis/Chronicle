// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore;

public class when_a_save_is_rejected : given.a_cache_encryption_key_store
{
    readonly EncryptionKeyIdentifier _identifier = "subject";
    readonly EncryptionKey _key = new([1], [2]);
    readonly Exception _rejection = new InvalidOperationException();
    Exception _error;
    EncryptionKey _result;

    void Establish() => _actualStore.SaveFor("store", "ns", _identifier, _key).ThrowsAsync(_rejection);

    async Task Because()
    {
        _error = await Catch.Exception(() => _store.SaveFor("store", "ns", _identifier, _key));
        _result = await _store.TryGetFor("store", "ns", _identifier);
    }

    [Fact] void should_propagate_the_rejection() => _error.ShouldBeSame(_rejection);
    [Fact] void should_not_cache_the_rejected_key() => _result.ShouldBeNull();
}
