// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;
using Cratis.Kernel.Storage.EventSequences;
using MongoDB.Driver;

namespace Cratis.Kernel.Storage.MongoDB;

#pragma warning disable CA1849, MA0042 // MongoDB breaks the Orleans task model internally, so it won't return to the task scheduler

/// <summary>
/// Represents an implementation of <see cref="IEventCursor"/> for handling events from event sequence.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventCursor"/> class.
/// </remarks>
/// <param name="converter"><see cref="IEventConverter"/> to convert event types.</param>
/// <param name="innerCursor">The underlying MongoDB cursor.</param>
/// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
public class EventCursor(
    IEventConverter converter,
    IAsyncCursor<Event>? innerCursor,
    CancellationToken cancellationToken = default) : IEventCursor
{
    readonly IEventConverter _converter = converter;
    readonly IAsyncCursor<Event>? _innerCursor = innerCursor;
    readonly CancellationToken _cancellationToken = cancellationToken;

    /// <inheritdoc/>
    public IEnumerable<AppendedEvent> Current { get; private set; } = Array.Empty<AppendedEvent>();

    /// <inheritdoc/>
    public async Task<bool> MoveNext()
    {
        if (_innerCursor is null) return false;
        if (_cancellationToken.IsCancellationRequested) return false;

        var result = _innerCursor.MoveNext(_cancellationToken);
        if (_innerCursor.Current is not null)
        {
            Current = (await Task.WhenAll(_innerCursor.Current.Select(_converter.ToAppendedEvent))).ToArray();
        }
        else
        {
            Current = Array.Empty<AppendedEvent>();
        }
        return result;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _innerCursor?.Dispose();
    }
}
