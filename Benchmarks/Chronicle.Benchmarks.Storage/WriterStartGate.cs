// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>Holds a wave's writers until all their tasks have been created.</summary>
public sealed class WriterStartGate
{
    readonly TaskCompletionSource _start = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Gets the stopwatch timestamp of the shared release.</summary>
    public long ReleasedAt { get; private set; }

    /// <summary>Waits for the shared release before entering a writer's timed section.</summary>
    /// <returns>The release task.</returns>
    public Task Wait() => _start.Task;

    /// <summary>Releases the writers and establishes the origin of their launch offsets.</summary>
    public void Release()
    {
        ReleasedAt = Stopwatch.GetTimestamp();
        _start.SetResult();
    }
}
