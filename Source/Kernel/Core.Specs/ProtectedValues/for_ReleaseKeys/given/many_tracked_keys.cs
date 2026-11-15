// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_ReleaseKeys.given;

public class many_tracked_keys : a_key_store
{
    protected readonly a_release_clock _clock = new();
    protected readonly List<TaskCompletionSource<EncryptionKeyErasure?>> _pending = [];
    readonly SemaphoreSlim _started = new(0);
    protected int _reads;

    protected void TrackKeys()
    {
        foreach (var index in Enumerable.Range(0, 64))
        {
            EncryptionKeyIdentifier identifier = $"subject-{index}";
            ReleaseKeys.Track(_keys, (EventStoreName.NotSet, EventStoreNamespaceName.NotSet, identifier), _pair);
            _keys.GetErasureForRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, identifier).Returns(_ =>
            {
                var pending = new TaskCompletionSource<EncryptionKeyErasure?>();
                _pending.Add(pending);
                _reads++;
                _started.Release();
                return pending.Task;
            });
        }
    }

    protected async Task AwaitBatch(int count = 32)
    {
        for (var index = 0; index < count; index++)
        {
            await _started.WaitAsync().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        }
    }

    protected void FinishBatch(EncryptionKeyErasure? fence = null)
    {
        var pending = _pending.ToArray();
        _pending.Clear();
        _clock.Advance(TimeSpan.FromSeconds(1));
        foreach (var read in pending) read.SetResult(fence);
    }

    void Destroy() => _started.Dispose();
}
