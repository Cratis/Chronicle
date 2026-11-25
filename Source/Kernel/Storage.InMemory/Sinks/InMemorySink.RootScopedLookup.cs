// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.InMemory.Sinks;

public partial class InMemorySink
{
    /// <inheritdoc/>
    public Task<Option<Key>> TryFindRootKeyByChildValueInRoot(PropertyPath childPropertyPath, object childValue, Key rootKey)
    {
        lock (_collectionLock)
        {
            var root = Collection.FirstOrDefault(entry => ValuesAreEqual(entry.Key, rootKey.Value));
            var found = root.Value is not null && TryFindValueInDocument(root.Value, childPropertyPath.Segments.ToArray(), 0, childValue);
            return Task.FromResult(found
                ? new Option<Key>(new Key(root.Key, ArrayIndexers.NoIndexers))
                : Option<Key>.None());
        }
    }
}
