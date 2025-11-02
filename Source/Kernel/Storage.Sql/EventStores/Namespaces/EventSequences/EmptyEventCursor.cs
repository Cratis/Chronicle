// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

/// <summary>
/// Represents an empty event cursor for methods not yet implemented.
/// </summary>
internal sealed class EmptyEventCursor : IEventCursor
{
    /// <inheritdoc/>
    public IEnumerable<AppendedEvent> Current { get; } = [];

    /// <inheritdoc/>
    public Task<bool> MoveNext()
    {
        return Task.FromResult(false);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Nothing to dispose
    }
}
