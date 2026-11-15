// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Memory;

/// <summary>
/// Defines a budget of memory that operations with a large, short lived footprint reserve before they run.
/// </summary>
/// <remarks>
/// <para>
/// Work that is cheap when it is small and expensive when it is large - converting a document, serializing a state,
/// materializing a batch - costs memory in proportion to its input, and nothing in the process stops many of them
/// from running at the same moment. When the sum of them outgrows the heap the allocation that fails is whichever
/// happens to be next, anywhere. A budget makes the work wait for room instead.
/// </para>
/// <para>
/// A reservation is an estimate, not a measurement: it is how much the operation expects to hold at its peak. Anything
/// that runs can be put behind it, so it is not tied to any one kind of work.
/// </para>
/// </remarks>
public interface IMemoryBudget
{
    /// <summary>
    /// Gets the number of bytes that can be reserved right now. It follows the memory pressure of the process.
    /// </summary>
    long Capacity { get; }

    /// <summary>
    /// Gets the number of bytes currently reserved.
    /// </summary>
    long Reserved { get; }

    /// <summary>
    /// Gets the number of reservations waiting for room.
    /// </summary>
    int Waiting { get; }

    /// <summary>
    /// Reserve memory, waiting until there is room for it.
    /// </summary>
    /// <param name="bytes">The number of bytes the operation expects to hold at its peak.</param>
    /// <param name="cancellationToken">Token to stop waiting.</param>
    /// <returns>The <see cref="MemoryReservation"/>, which gives the memory back when disposed.</returns>
    /// <remarks>
    /// A request larger than the whole budget is not refused - it waits until nothing else is reserved and then runs
    /// alone. Requests are served in the order they arrive, so a large one is not starved by small ones.
    /// </remarks>
    ValueTask<MemoryReservation> Reserve(long bytes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reserve memory only if there is room for it right now.
    /// </summary>
    /// <param name="bytes">The number of bytes the operation expects to hold at its peak.</param>
    /// <param name="reservation">The <see cref="MemoryReservation"/> when there was room.</param>
    /// <returns>True when the memory was reserved; false when it was not.</returns>
    bool TryReserve(long bytes, out MemoryReservation reservation);
}
