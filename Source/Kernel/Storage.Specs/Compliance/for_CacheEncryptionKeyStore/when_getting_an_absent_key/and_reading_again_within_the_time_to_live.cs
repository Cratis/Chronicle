// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore.when_getting_an_absent_key;

public class and_reading_again_within_the_time_to_live : given.a_cache_over_a_store_without_the_key
{
    EncryptionKey? _result;

    async Task Because()
    {
        await _store.TryGetFor(string.Empty, string.Empty, _identifier);
        _timeProvider.Advance(_timeToLive - TimeSpan.FromSeconds(1));
        _result = await _store.TryGetFor(string.Empty, string.Empty, _identifier);
    }

    [Fact] void should_query_the_actual_store_only_once() => _actualStore.Received(1).TryGetFor(string.Empty, string.Empty, _identifier);
    [Fact] void should_return_no_key() => _result.ShouldBeNull();
}
