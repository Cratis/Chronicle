// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_KernelHost.given;

internal sealed class held_active_stage(TimeProvider? clock = null) : ILifecycleParticipant<ISiloLifecycle>
{
    internal TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal held_silo_startup_diagnostics Diagnostics { get; } = new(clock ?? TimeProvider.System);
    internal bool Armed { get; private set; }

    public void Participate(ISiloLifecycle lifecycle)
    {
        lifecycle.Subscribe(nameof(held_active_stage), ServiceLifecycleStage.Active, async token =>
        {
            Diagnostics.Record("Active hold entered");
            Entered.TrySetResult();
            await Release.Task.WaitAsync(token);
            Diagnostics.Record("Active hold released");
        });
        Armed = true;
        Diagnostics.Record("Active hold armed");
    }
}
