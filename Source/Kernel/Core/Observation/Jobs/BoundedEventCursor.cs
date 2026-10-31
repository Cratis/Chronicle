// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Wraps an <see cref="IEventCursor"/> so that no batch it exposes holds more than a fixed number of events,
/// whatever size the storage hands back.
/// </summary>
/// <remarks>
/// The storage cursors return batches of very different sizes: a MongoDB getMore batch is limited only by
/// 16 MiB, and the SQL storage reads in blocks of its own. A job step that delivers each batch to the subscriber in
/// one call then carries the whole batch inside a single subscriber timeout. Slicing the batch lets the step report
/// and checkpoint after every slice, so progress from slices the subscriber acknowledged is kept and a timeout puts
/// at most the slice in flight at risk of being delivered again.
/// </remarks>
internal sealed class BoundedEventCursor : IEventCursor
{
    /// <summary>
    /// The default most events one subscriber call carries. It matches the default
    /// <see cref="Cratis.Chronicle.Configuration.ReadModels.MaxBatchSize"/>, so the size of a catch-up call and of a
    /// replay page agree.
    /// </summary>
    internal const int DefaultMaximumEventsPerBatch = 500;

    readonly IEventCursor _inner;
    readonly int _maximumEventsPerBatch;
    AppendedEvent[] _buffer = [];
    int _position;

    /// <summary>
    /// Initializes a new instance of the <see cref="BoundedEventCursor"/> class.
    /// </summary>
    /// <param name="inner">The cursor to slice.</param>
    /// <param name="maximumEventsPerBatch">The most events <see cref="Current"/> may hold.</param>
    /// <exception cref="ArgumentOutOfRangeException">The bound is below one.</exception>
    internal BoundedEventCursor(IEventCursor inner, int maximumEventsPerBatch)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumEventsPerBatch, 1);
        _inner = inner;
        _maximumEventsPerBatch = maximumEventsPerBatch;
    }

    /// <inheritdoc/>
    public IEnumerable<AppendedEvent> Current { get; private set; } = [];

    /// <inheritdoc/>
    public async Task<bool> MoveNext()
    {
        if (_position >= _buffer.Length)
        {
            if (!await _inner.MoveNext())
            {
                Current = [];
                _buffer = [];
                _position = 0;
                return false;
            }

            // An empty inner batch is passed through as one empty slice, as the job steps handle it today.
            _buffer = [.. _inner.Current];
            _position = 0;
            if (_buffer.Length == 0)
            {
                Current = [];
                return true;
            }
        }

        var count = Math.Min(_maximumEventsPerBatch, _buffer.Length - _position);
        Current = _position == 0 && count == _buffer.Length ? _buffer : _buffer.AsSpan(_position, count).ToArray();
        _position += count;
        return true;
    }

    /// <inheritdoc/>
    public void Dispose() => _inner.Dispose();
}
