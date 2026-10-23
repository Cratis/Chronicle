// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_KernelHost.given;

internal sealed class held_active_stage : ILifecycleParticipant<ISiloLifecycle>
{
    internal TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void Participate(ISiloLifecycle lifecycle) => lifecycle.Subscribe(nameof(held_active_stage), ServiceLifecycleStage.Active, async token =>
    {
        Entered.TrySetResult();
        await Release.Task.WaitAsync(token);
    });
}
