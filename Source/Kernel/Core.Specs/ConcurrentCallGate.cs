// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Records whether an expected number of asynchronous calls were in flight at the same time.
/// </summary>
/// <param name="expectedCalls">The number of calls expected to be in flight together.</param>
/// <remarks>
/// Every call reports its arrival through <see cref="Enter"/>, which completes once the last expected call has arrived.
/// Calls issued one after the other leave through the timeout instead, which is what makes this discriminate between a
/// fan-out and a sequential loop.
/// </remarks>
public sealed class ConcurrentCallGate(int expectedCalls)
{
    static readonly TimeSpan _waitForRemainingCalls = TimeSpan.FromSeconds(2);

    readonly TaskCompletionSource _allCallsArrived = new();
    int _arrivedCalls;
    int _callsThatLeftThroughTheGate;

    /// <summary>
    /// Gets a value indicating whether every expected call was in flight at the same time.
    /// </summary>
    public bool AllCallsWereConcurrent => _callsThatLeftThroughTheGate == expectedCalls;

    /// <summary>
    /// Report that a call has started and wait for the remaining expected calls to arrive.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    public async Task Enter()
    {
        if (Interlocked.Increment(ref _arrivedCalls) == expectedCalls)
        {
            _allCallsArrived.TrySetResult();
        }

        var left = await Task.WhenAny(_allCallsArrived.Task, Task.Delay(_waitForRemainingCalls));
        if (left == _allCallsArrived.Task)
        {
            Interlocked.Increment(ref _callsThatLeftThroughTheGate);
        }
    }
}
