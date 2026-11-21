// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Lets only one read of a persistent state be in flight at a time.
/// </summary>
/// <remarks>
/// Orleans refuses a read of a persistent state while another read of it is in flight. An observer reads the
/// same state from turns that can interleave, so each read waits for the one before it and then reads fresh -
/// sharing the earlier read would hand back data older than something the caller has since written or moved.
/// One instance guards one state; reads of different states do not wait for each other. The gate is never disposed:
/// an activation can be torn down while a read is in flight or queued, and disposing a semaphore under a holder
/// makes its release throw and strands its waiters, so it is an <see cref="AsyncGate"/>, which has nothing to dispose.
/// </remarks>
internal sealed class SerializedStateReads
{
    readonly AsyncGate _gate = new();

    /// <summary>
    /// Performs a read once no other read through this instance is in flight.
    /// </summary>
    /// <param name="read">The read to perform.</param>
    /// <returns>Awaitable task.</returns>
    public async Task Read(Func<Task> read)
    {
        await _gate.WaitAsync();
        try
        {
            await read();
        }
        finally
        {
            _gate.Release();
        }
    }
}
