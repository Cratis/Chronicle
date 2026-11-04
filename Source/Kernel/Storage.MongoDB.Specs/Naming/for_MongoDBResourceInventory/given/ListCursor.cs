// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBResourceInventory.given;

/// <summary>
/// Represents a cursor over a fixed list, delivered as one batch.
/// </summary>
/// <typeparam name="T">The type of item.</typeparam>
/// <param name="items">The items.</param>
public sealed class ListCursor<T>(IEnumerable<T> items) : IAsyncCursor<T>
{
    bool _delivered;

    /// <inheritdoc/>
    public IEnumerable<T> Current { get; private set; } = [];

    /// <inheritdoc/>
    public bool MoveNext(CancellationToken cancellationToken = default) => Advance();

    /// <inheritdoc/>
    public Task<bool> MoveNextAsync(CancellationToken cancellationToken = default) => Task.FromResult(Advance());

    bool Advance()
    {
        if (_delivered)
        {
            return false;
        }

        _delivered = true;
        Current = items;
        return true;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
