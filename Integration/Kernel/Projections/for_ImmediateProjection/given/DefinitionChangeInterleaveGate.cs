// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Kernel.Integration.Projections.for_ImmediateProjection.given;

/// <summary>
/// Holds one outgoing fold until the definition change has completed.
/// </summary>
public class DefinitionChangeInterleaveGate
{
    readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _armed;

    /// <summary>
    /// Gets the signal that the read has reached its fold.
    /// </summary>
    public Task Entered => _entered.Task;

    /// <summary>
    /// Arms the gate for the next fold.
    /// </summary>
    public void Arm() => Interlocked.Exchange(ref _armed, 1);

    /// <summary>
    /// Attempts to hold the next fold.
    /// </summary>
    /// <returns>Whether this invocation was selected.</returns>
    public bool TryEnter()
    {
        if (Interlocked.CompareExchange(ref _armed, 0, 1) != 1)
        {
            return false;
        }

        _entered.TrySetResult();
        return true;
    }

    /// <summary>
    /// Waits until the test releases the fold.
    /// </summary>
    /// <returns>The bounded wait.</returns>
    public Task WaitForRelease() => _release.Task.WaitAsync(TimeSpan.FromSeconds(30));

    /// <summary>
    /// Releases the held fold, including when the test fails.
    /// </summary>
    public void Release() => _release.TrySetResult();
}
