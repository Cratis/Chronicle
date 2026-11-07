// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Memory;

/// <summary>
/// Represents memory reserved from an <see cref="IMemoryBudget"/>.
/// </summary>
public sealed class MemoryReservation : IDisposable
{
    /// <summary>
    /// Gets a reservation that holds nothing and is never counted.
    /// </summary>
    public static readonly MemoryReservation None = new(null, 0);

    readonly MemoryBudget? _budget;
    long _bytes;
    int _released;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryReservation"/> class.
    /// </summary>
    /// <param name="budget">The <see cref="MemoryBudget"/> the memory was reserved from.</param>
    /// <param name="bytes">The number of bytes reserved.</param>
    internal MemoryReservation(MemoryBudget? budget, long bytes)
    {
        _budget = budget;
        _bytes = bytes;
    }

    /// <summary>
    /// Gets the number of bytes held.
    /// </summary>
    public long Bytes => Interlocked.Read(ref _bytes);

    /// <summary>
    /// Account for more memory than was reserved, once the real size of the work is known.
    /// </summary>
    /// <param name="additionalBytes">The number of bytes to add.</param>
    /// <remarks>
    /// This never waits, because the memory is already in use: it makes everything that reserves afterwards
    /// wait for room that is really there. It can take the budget over its capacity.
    /// </remarks>
    public void Grow(long additionalBytes)
    {
        if (additionalBytes <= 0 || _budget is null || Volatile.Read(ref _released) != 0)
        {
            return;
        }

        Interlocked.Add(ref _bytes, additionalBytes);
        _budget.Account(additionalBytes);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_budget is null || Interlocked.Exchange(ref _released, 1) != 0)
        {
            return;
        }

        _budget.Release(Interlocked.Read(ref _bytes));
    }
}
