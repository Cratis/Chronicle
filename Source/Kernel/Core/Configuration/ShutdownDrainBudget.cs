// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Shares one stop deadline across observer services and appended event queues.
/// </summary>
[Singleton]
public class ShutdownDrainBudget : IShutdownDrainBudget, IDisposable
{
    readonly CancellationTokenSource _deadline = new();
    readonly CancellationTokenRegistration _stopping;
    int _started;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShutdownDrainBudget"/> class.
    /// </summary>
    /// <param name="options">The Chronicle options.</param>
    /// <param name="hostOptions">The host shutdown timeout.</param>
    /// <param name="lifetime">The host lifetime.</param>
    /// <param name="logger">The logger.</param>
    public ShutdownDrainBudget(IOptions<ChronicleOptions> options, IOptions<HostOptions> hostOptions, IHostApplicationLifetime lifetime, ILogger<ShutdownDrainBudget> logger)
    {
        Timeout = EffectiveTimeout(options.Value.Shutdown.DrainTimeout, hostOptions.Value.ShutdownTimeout, options.Value.Shutdown.HostShutdownReserve);
        logger.EffectiveDrainBudget(Timeout);
        _stopping = lifetime.ApplicationStopping.Register(() => Begin());
    }

    /// <inheritdoc/>
    public CancellationToken Token => _deadline.Token;

    /// <inheritdoc/>
    public bool IsStopping => Volatile.Read(ref _started) != 0;

    /// <inheritdoc/>
    public TimeSpan Timeout { get; }

    /// <inheritdoc/>
    public CancellationToken Begin()
    {
        if (Interlocked.Exchange(ref _started, 1) == 0)
        {
            _deadline.CancelAfter(Timeout);
        }

        return Token;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _stopping.Dispose();
        _deadline.Dispose();
    }

    /// <summary>
    /// Computes the budget while preserving the configured host shutdown reserve.
    /// </summary>
    /// <param name="configured">The configured drain timeout.</param>
    /// <param name="hostTimeout">The host shutdown timeout.</param>
    /// <param name="hostShutdownReserve">The time reserved for host shutdown after draining.</param>
    /// <returns>The nonnegative effective timeout.</returns>
    internal static TimeSpan EffectiveTimeout(TimeSpan configured, TimeSpan hostTimeout, TimeSpan hostShutdownReserve) =>
        TimeSpan.FromTicks(Math.Max(0, Math.Min(configured.Ticks, hostTimeout.Ticks - hostShutdownReserve.Ticks)));
}
