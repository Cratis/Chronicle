// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore.given;

public class an_in_flight_key_read : a_cache_with_present_keys
{
    protected readonly TaskCompletionSource<EncryptionKey?> _read = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly ControllableTimeProvider _time = new();
    protected EncryptionKey? _result;
    protected EncryptionKey? _later;

    void Establish()
    {
        _store = new(_actualStore, _time);
        _actualStore.TryGetFor("store", "ns", "subject").Returns(_read.Task);
    }

    protected async Task EraseSubject()
    {
        await _store.RecordErasureFor("store", "ns", "subject");
        _actualStore.TryGetFor("store", "ns", "subject").Returns((EncryptionKey?)null);
    }

    protected async Task CompleteRead(Task<EncryptionKey?> pending)
    {
        _read.SetResult(_key);
        _result = await pending;
        _time.Advance(CacheEncryptionKeyStorage.DefaultNegativeCacheTimeToLive + TimeSpan.FromSeconds(1));
        _later = await _store.TryGetFor("store", "ns", "subject");
    }
}
