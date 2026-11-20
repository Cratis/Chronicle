// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Chronicle.Configuration;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the silo-wide implementation of <see cref="IWakeAdmission"/>.
/// </summary>
/// <remarks>
/// The default limit is the one that bounds subscriptions (<see cref="SubscriptionAdmission.ResolveLimit(int, int)"/>): two per
/// processor, at least 4 and at most 16. A wake reads the observer's state and a bounded part of the log and calls a client,
/// the same kind of work a subscription does, so the same bound keeps the same load off storage and clients. It is deliberately
/// not larger: the wait is short because a wake holds its place only while the observer activates and resumes, and a longer
/// queue of wakes is what a wake storm should look like.
/// </remarks>
[Singleton]
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "The silo gate never uses AvailableWaitHandle; retaining the managed semaphore avoids disposal racing releases.")]
public class WakeAdmission : IWakeAdmission
{
    readonly SemaphoreSlim _admission;

    /// <summary>
    /// Initializes a new instance of the <see cref="WakeAdmission"/> class.
    /// </summary>
    /// <param name="options">The <see cref="ChronicleOptions"/>.</param>
    public WakeAdmission(IOptions<ChronicleOptions> options)
        : this(options.Value.Observers.MaxConcurrentWakes, Environment.ProcessorCount)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WakeAdmission"/> class.
    /// </summary>
    /// <param name="configuredLimit">The configured limit; below 1 selects the automatic limit.</param>
    /// <param name="processorCount">The number of processors the automatic limit is derived from.</param>
    internal WakeAdmission(int configuredLimit, int processorCount)
    {
        Limit = SubscriptionAdmission.ResolveLimit(configuredLimit, processorCount);
        _admission = new(Limit, Limit);
    }

    /// <inheritdoc/>
    public int Limit { get; }

    /// <inheritdoc/>
    public int InProgress => Limit - _admission.CurrentCount;

    /// <inheritdoc/>
    public Task WaitAsync(CancellationToken cancellationToken) => _admission.WaitAsync(cancellationToken);

    /// <inheritdoc/>
    public void Release() => _admission.Release();
}
