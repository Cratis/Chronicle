// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Optional capability of a sink to read and write the instances of a page in as few round trips as it can, without a bulk window.
/// </summary>
/// <remarks>
/// A sink is one shared instance per read model and namespace, so its bulk window is shared by everything writing through it:
/// a write made while a window is open is buffered in it and only reaches the store when the window is flushed. A caller that
/// must know its writes are durable when it returns, without owning that shared window, uses this capability instead. A sink
/// without it is read and written one instance at a time.
/// </remarks>
public interface ISinkPages
{
    /// <summary>
    /// Finds the instances of a page.
    /// </summary>
    /// <param name="keys">The <see cref="Key"/> of every instance in the page.</param>
    /// <returns>The instance of each key, or null when it has none, in the order of <paramref name="keys"/>.</returns>
    /// <remarks>
    /// Answers exactly as <see cref="ISink.FindOrDefault(Key)"/> answers for each key, including from an open bulk window.
    /// </remarks>
    Task<IReadOnlyList<ExpandoObject?>> FindPage(IReadOnlyList<Key> keys);

    /// <summary>
    /// Applies the changes of a page, and returns once they are durable.
    /// </summary>
    /// <param name="pageWrites">The <see cref="SinkPageWrite"/> of every instance in the page.</param>
    /// <returns>The partitions whose writes failed; every other write has been applied.</returns>
    /// <remarks>
    /// Each write is applied exactly as <see cref="ISink.ApplyChanges(Key, IChangeset{AppendedEvent, ExpandoObject}, EventSequenceNumber, SinkWriteMode)"/>
    /// applies it. The writes are not buffered in a bulk window and never open, flush or close one, so the outcome of every
    /// write is known when this returns, and a failed write fails only its own partition. While a window is open, for
    /// instance by a replay, the writes join it as <see cref="ISink.ApplyChanges(Key, IChangeset{AppendedEvent, ExpandoObject}, EventSequenceNumber, SinkWriteMode)"/>
    /// would.
    /// </remarks>
    Task<IEnumerable<FailedPartition>> ApplyPage(IReadOnlyList<SinkPageWrite> pageWrites);
}
