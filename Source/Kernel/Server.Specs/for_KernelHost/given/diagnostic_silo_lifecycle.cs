// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Server.for_KernelHost.given;

/// <summary>
/// Preserves Orleans' lifecycle behavior, adding one non-blocking entry marker per subscribed stage,
/// including custom stages. Does not record observer names or formatted log messages.
/// </summary>
/// <param name="logger">The logger for the lifecycle subject.</param>
/// <param name="localSiloDetails">The local silo details.</param>
/// <param name="diagnostics">The diagnostics that receive the stage entry markers.</param>
internal sealed class diagnostic_silo_lifecycle(
    ILogger<SiloLifecycleSubject> logger,
    ILocalSiloDetails localSiloDetails,
    held_silo_startup_diagnostics diagnostics) : SiloLifecycleSubject(logger, localSiloDetails)
{
    readonly HashSet<int> _stages = [];

    public override IDisposable Subscribe(string observerName, int stage, ILifecycleObserver observer)
    {
        if (_stages.Add(stage))
        {
            var name = GetStageName(stage);
            base.Subscribe(nameof(diagnostic_silo_lifecycle), stage, new stage_marker(() => diagnostics.StageReached(stage, name)));
        }

        return base.Subscribe(observerName, stage, observer);
    }

    sealed class stage_marker(Action entered) : ILifecycleObserver
    {
        public Task OnStart(CancellationToken cancellationToken = default)
        {
            entered();
            return Task.CompletedTask;
        }

        public Task OnStop(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
