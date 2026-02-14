// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Keys;

namespace Cratis.Chronicle.Observation.Jobs;

public class ObserverKeysForTesting(IEnumerable<Key> keys) : IObserverKeys
{
    readonly IEnumerable<Key> _keys = keys;

    public async IAsyncEnumerator<Key> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        foreach (var key in _keys)
        {
            yield return key;
        }
        await Task.CompletedTask;
    }
}
