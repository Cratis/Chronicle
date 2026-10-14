// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints;

/// <summary>
/// Reads values under the index lock one move at a time, without holding a monitor across an await.
/// </summary>
/// <param name="sync">The index lock.</param>
/// <param name="values">The deferred source query.</param>
/// <param name="readToken">The read deadline.</param>
internal sealed class UniqueConstraintValues(object sync, Func<IEnumerator<UniqueConstraintValue>> values, CancellationToken readToken) : IAsyncEnumerable<UniqueConstraintValue>
{
    /// <inheritdoc/>
    public IAsyncEnumerator<UniqueConstraintValue> GetAsyncEnumerator(CancellationToken cancellationToken = default) => new Cursor(sync, values(), readToken, cancellationToken);

    sealed class Cursor(object sync, IEnumerator<UniqueConstraintValue> values, CancellationToken readToken, CancellationToken consumerToken) : IAsyncEnumerator<UniqueConstraintValue>
    {
        /// <inheritdoc/>
        public UniqueConstraintValue Current => values.Current;

        /// <inheritdoc/>
        public ValueTask<bool> MoveNextAsync()
        {
            readToken.ThrowIfCancellationRequested();
            consumerToken.ThrowIfCancellationRequested();
            lock (sync)
            {
                return ValueTask.FromResult(values.MoveNext());
            }
        }

        /// <inheritdoc/>
        public ValueTask DisposeAsync()
        {
            lock (sync)
            {
                values.Dispose();
            }
            return ValueTask.CompletedTask;
        }
    }
}
