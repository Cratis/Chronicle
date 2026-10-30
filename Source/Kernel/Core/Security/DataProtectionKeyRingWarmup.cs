// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Defers the framework key-ring warmup until the silo can route grain calls.
/// </summary>
/// <remarks>
/// Marked with <see cref="IgnoreConventionAttribute"/> so convention binding does not self-bind it. Its
/// constructor takes the plain framework <see cref="IHostedService"/> it wraps, which convention binding
/// would resolve as "whichever hosted service happens to be registered last" rather than the intended
/// Data Protection warmup - <see cref="ChronicleDataProtection.DeferChronicleDataProtectionWarmup"/> is the
/// only place that may construct one, with the exact instance to wrap.
/// </remarks>
/// <param name="warmup">The framework hosted service.</param>
[IgnoreConvention]
public sealed class DataProtectionKeyRingWarmup(IHostedService warmup) : IHostedLifecycleService
{
    bool _deferredToSilo;

    /// <summary>
    /// Transfers warmup ownership to the listener-first silo adapter before host startup.
    /// </summary>
    public void DeferToSiloStart() => _deferredToSilo = true;

    /// <summary>
    /// Runs the original framework warmup after silo startup completes.
    /// </summary>
    /// <param name="cancellationToken">The startup cancellation token.</param>
    /// <returns>The framework startup task.</returns>
    public Task Warmup(CancellationToken cancellationToken) => warmup.StartAsync(cancellationToken);

    /// <inheritdoc/>
    public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StartedAsync(CancellationToken cancellationToken) => _deferredToSilo ? Task.CompletedTask : Warmup(cancellationToken);

    /// <inheritdoc/>
    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => warmup.StopAsync(cancellationToken);

    /// <inheritdoc/>
    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
