// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Chronicle.Configuration;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents an implementation of <see cref="ISubscriptionAdmission"/> bounding subscription work for the whole silo.
/// </summary>
/// <remarks>
/// No wait handle is used. Managed lifetime deliberately avoids disposal racing another activation's release.
/// </remarks>
[Singleton]
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "The silo gate never uses AvailableWaitHandle; retaining the managed semaphore avoids disposal racing activation releases.")]
public class SubscriptionAdmission : ISubscriptionAdmission
{
    /// <summary>
    /// The fewest operations the automatic limit admits, however small the machine.
    /// </summary>
    internal const int MinimumAutomaticLimit = 4;

    /// <summary>
    /// The most operations the automatic limit admits, however large the machine.
    /// </summary>
    internal const int MaximumAutomaticLimit = 16;

    readonly SemaphoreSlim _admission;
    int _recoveriesAdmitted;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubscriptionAdmission"/> class.
    /// </summary>
    /// <param name="options">The <see cref="ChronicleOptions"/> holding <see cref="Configuration.Observers.MaxConcurrentSubscriptions"/>.</param>
    public SubscriptionAdmission(IOptions<ChronicleOptions> options)
        : this(options.Value.Observers.MaxConcurrentSubscriptions, Environment.ProcessorCount)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SubscriptionAdmission"/> class.
    /// </summary>
    /// <param name="configuredLimit">The configured limit; zero or less selects the automatic one.</param>
    /// <param name="processorCount">The processors available to the process.</param>
    internal SubscriptionAdmission(int configuredLimit, int processorCount)
    {
        Limit = ResolveLimit(configuredLimit, processorCount);
        RecoveryLimit = Math.Max(1, Limit / 2);
        _admission = new(Limit, Limit);
    }

    /// <inheritdoc/>
    public int Limit { get; }

    /// <inheritdoc/>
    public int RecoveryLimit { get; }

    /// <inheritdoc/>
    public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken) => _admission.WaitAsync(timeout, cancellationToken);

    /// <inheritdoc/>
    public bool TryEnterForRecovery()
    {
        if (Interlocked.Increment(ref _recoveriesAdmitted) <= RecoveryLimit && _admission.Wait(0))
        {
            return true;
        }

        Interlocked.Decrement(ref _recoveriesAdmitted);
        return false;
    }

    /// <inheritdoc/>
    public void Release() => _admission.Release();

    /// <inheritdoc/>
    public void ReleaseRecovery()
    {
        _admission.Release();
        Interlocked.Decrement(ref _recoveriesAdmitted);
    }

    /// <summary>
    /// Resolves the effective limit.
    /// </summary>
    /// <param name="configuredLimit">The configured limit; zero or less selects the automatic one.</param>
    /// <param name="processorCount">The processors available to the process.</param>
    /// <returns>The limit to admit.</returns>
    /// <remarks>
    /// The automatic limit is two per processor, so a 2-core pod and a 32-core pod do not share one constant, kept
    /// within <see cref="MinimumAutomaticLimit"/> and <see cref="MaximumAutomaticLimit"/>: subscription work is mostly
    /// storage and grain calls, and more of it at once than the storage pool can serve only queues there instead.
    /// </remarks>
    internal static int ResolveLimit(int configuredLimit, int processorCount) => configuredLimit > 0
        ? configuredLimit
        : Math.Clamp(processorCount * 2, MinimumAutomaticLimit, MaximumAutomaticLimit);
}
