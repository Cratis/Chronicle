// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Slices.for_ReactionSlice.given;

public sealed class ReactorControl : IDisposable
{
    public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public List<string> Inputs { get; } = [];
    public bool Fail { get; set; }
    public int Disposals { get; private set; }
    public void Dispose() => Disposals++;
}
