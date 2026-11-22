// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Coordinates lifecycle work outside the measured call.
/// </summary>
/// <param name="prime">Calls and validates the selected target.</param>
/// <param name="deactivate">Awaits exact target deactivation.</param>
/// <param name="restart">Replaces the host and refreshes references.</param>
/// <param name="capture">Checks and captures the pre-call lifecycle.</param>
public sealed class ActivationIteration(Func<Task> prime, Func<Task> deactivate, Func<Task> restart, Action capture)
{
    bool _prepared;

    /// <summary>Prepares one operation; failures disallow validation as a measurement.</summary>
    /// <param name="mode">The lifecycle to exercise.</param>
    /// <returns>The preparation task.</returns>
    public async Task Prepare(GrainActivationMode mode)
    {
        _prepared = false;
        switch (mode)
        {
            case GrainActivationMode.Warm:
                await prime();
                break;
            case GrainActivationMode.Cold:
                await prime();
                await deactivate();
                break;
            case GrainActivationMode.PostRestart:
                await restart();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        }

        capture();
        _prepared = true;
    }

    /// <summary>Validates a completed operation only after successful preparation.</summary>
    /// <param name="verify">The lifecycle and result assertions.</param>
    /// <exception cref="InvalidOperationException">Preparation did not complete.</exception>
    public void Verify(Action verify)
    {
        if (!_prepared) throw new InvalidOperationException("Activation iteration was not prepared.");
        _prepared = false;
        verify();
    }
}
