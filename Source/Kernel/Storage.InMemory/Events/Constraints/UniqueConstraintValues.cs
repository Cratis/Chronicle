// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints;

/// <summary>
/// Streams a snapshot of held values without observing concurrent index mutations.
/// </summary>
/// <param name="values">The values copied under the index lock.</param>
/// <param name="readToken">The read deadline.</param>
internal sealed class UniqueConstraintValues(UniqueConstraintValue[] values, CancellationToken readToken) : IAsyncEnumerable<UniqueConstraintValue>
{
    /// <inheritdoc/>
    public IAsyncEnumerator<UniqueConstraintValue> GetAsyncEnumerator(CancellationToken cancellationToken = default) => new Cursor(((IEnumerable<UniqueConstraintValue>)values).GetEnumerator(), readToken, cancellationToken);

    sealed class Cursor(IEnumerator<UniqueConstraintValue> values, CancellationToken readToken, CancellationToken consumerToken) : IAsyncEnumerator<UniqueConstraintValue>
    {
        /// <inheritdoc/>
        public UniqueConstraintValue Current => values.Current;

        /// <inheritdoc/>
        public ValueTask<bool> MoveNextAsync()
        {
            readToken.ThrowIfCancellationRequested();
            consumerToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(values.MoveNext());
        }

        /// <inheritdoc/>
        public ValueTask DisposeAsync()
        {
            values.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
