// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameReservations;

public class FakeReservations : ResourceNameReservations
{
    readonly ConcurrentDictionary<string, string> _held = new();

    public string? FailOnKey { get; set; }

    public IReadOnlyDictionary<string, string> Held => _held;

    protected override Task<string?> TryAdd(string key, string owner)
    {
        if (key == FailOnKey)
        {
            throw new InvalidOperationException("The store failed.");
        }

        return Task.FromResult(_held.TryAdd(key, owner) ? null : _held[key]);
    }

    protected override Task Remove(IReadOnlyCollection<string> keys, string owner)
    {
        foreach (var key in keys)
        {
            _held.TryRemove(new KeyValuePair<string, string>(key, owner));
        }

        return Task.CompletedTask;
    }
}
