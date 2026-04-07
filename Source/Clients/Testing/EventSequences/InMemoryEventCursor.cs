// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using Cratis.Chronicle.Storage.EventSequences;
using KernelAppendedEvent = KernelConcepts::Cratis.Chronicle.Concepts.Events.AppendedEvent;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents an in-memory implementation of <see cref="IEventCursor"/> backed by a pre-fetched list.
/// </summary>
/// <param name="events">The events this cursor will iterate over.</param>
internal sealed class InMemoryEventCursor(IList<KernelAppendedEvent> events) : IEventCursor
{
    const int PageSize = 100;
    int _pageStart = -1;

    /// <inheritdoc/>
    public IEnumerable<KernelAppendedEvent> Current =>
        _pageStart < 0 ? [] : events.Skip(_pageStart).Take(PageSize);

    /// <inheritdoc/>
    public Task<bool> MoveNext()
    {
        var next = _pageStart < 0 ? 0 : _pageStart + PageSize;
        if (next >= events.Count)
        {
            return Task.FromResult(false);
        }

        _pageStart = next;
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
