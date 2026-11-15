// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues;

/// <summary>A single release validation's cancellation and overall refresh budget.</summary>
/// <param name="clock">The operation's clock and delay provider.</param>
/// <param name="timeout">The maximum time spent validating, including all retries.</param>
/// <param name="cancellationToken">Cancellation of the release.</param>
readonly struct ReleaseAttempt(TimeProvider clock, TimeSpan timeout, CancellationToken cancellationToken)
{
    readonly long _started = clock.GetTimestamp();
    readonly DateTimeOffset _wallStarted = clock.GetUtcNow();

    /// <summary>Refuse validation after cancellation or the overall deadline.</summary>
    public void Check() => Remaining();

    /// <summary>Wait only within the remaining validation budget.</summary>
    /// <typeparam name="T">The read result.</typeparam>
    /// <param name="task">The pending provider read.</param>
    /// <returns>The completed read result.</returns>
    /// <exception cref="EncryptionKeyFenceUnavailable">The overall validation deadline expired.</exception>
    public async Task<T> Wait<T>(Task<T> task)
    {
        try
        {
            var result = await task.WaitAsync(Remaining(), clock, cancellationToken);
            Check();
            return result;
        }
        catch (TimeoutException error)
        {
            throw new EncryptionKeyFenceUnavailable(error);
        }
    }

    TimeSpan Remaining()
    {
        cancellationToken.ThrowIfCancellationRequested();
        var elapsed = clock.GetElapsedTime(_started);
        var wallElapsed = clock.GetUtcNow() - _wallStarted;
        var remaining = timeout - (elapsed > wallElapsed ? elapsed : wallElapsed);
        if (remaining <= TimeSpan.Zero) throw new EncryptionKeyFenceUnavailable(new TimeoutException());
        return remaining;
    }
}
