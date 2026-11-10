// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_reducing_a_page.given;

/// <summary>
/// A <see cref="PageSink"/> that can read and write a page without a bulk window, recording how it is used.
/// </summary>
public class PageSinkWithPageWrites : PageSink, ISinkPages
{
    /// <summary>
    /// Gets the keys of every page read, in call order.
    /// </summary>
    public List<Key[]> PageReads { get; } = [];

    /// <summary>
    /// Gets the partitions of every page write, in call order.
    /// </summary>
    public List<string[]> PageWrites { get; } = [];

    /// <summary>
    /// Gets or sets the exception a page write throws, if any.
    /// </summary>
    public Exception? PageWriteError { get; set; }

    /// <inheritdoc/>
    public Task<IReadOnlyList<ExpandoObject?>> FindPage(IReadOnlyList<Key> keys)
    {
        Calls.Add(nameof(FindPage));
        PageReads.Add([.. keys]);
        return Task.FromResult<IReadOnlyList<ExpandoObject?>>([.. keys.Select(key => States.TryGetValue(key.Value.ToString()!, out var state) ? state : null)]);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> ApplyPage(IReadOnlyList<SinkPageWrite> pageWrites)
    {
        Calls.Add(nameof(ApplyPage));
        var partitions = pageWrites.Select(write => write.Key.Value.ToString()!).ToArray();
        PageWrites.Add(partitions);
        if (PageWriteError is { } error)
        {
            return Task.FromException<IEnumerable<FailedPartition>>(error);
        }

        Writes.AddRange(partitions);
        Durable.AddRange(partitions.Where(partition => !FailingWrites.Contains(partition)));
        return Task.FromResult<IEnumerable<FailedPartition>>([.. pageWrites
            .Where(write => FailingWrites.Contains(write.Key.Value.ToString()!))
            .Select(write => new FailedPartition(write.Key, write.EventSequenceNumber) { Reason = "write failed" })]);
    }
}
