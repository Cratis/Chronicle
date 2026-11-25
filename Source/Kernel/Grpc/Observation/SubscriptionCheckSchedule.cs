// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Services.Observation;

/// <summary>
/// Decides when the subscription of one connected client is checked next.
/// </summary>
/// <remarks>
/// <para>
/// A subscription that is not confirmed intact - one that was just registered, one that was just restored, one whose
/// check failed - is checked every revise interval, so a restore is confirmed or retried quickly. Otherwise a
/// subscription known to be intact is only checked at the safety-net interval. The checks after a silo starts leaving
/// the cluster, which is what loses subscriptions, are not decided here: the silo's supervisor asks for them.
/// </para>
/// <para>
/// The safety-net wait is drawn anew for every check, between three quarters of the interval and the whole interval.
/// Clients register their observers together and reconnect together after a kernel restart; without the jitter every
/// subscription would be checked in the same second of every interval. The interval stays the upper bound.
/// </para>
/// <para>
/// One schedule belongs to one keeper loop and is not safe to share between threads.
/// </para>
/// </remarks>
internal sealed class SubscriptionCheckSchedule
{
    /// <summary>
    /// The share of the safety-net interval that is always waited; the rest is drawn at random.
    /// </summary>
    internal const double SafetyNetFloor = 0.75;

    /// <summary>
    /// The shortest revise interval the schedule uses; a shorter one would make the keeper a tight loop of grain calls.
    /// </summary>
    internal static readonly TimeSpan MinimumRevise = TimeSpan.FromSeconds(1);

    readonly TimeSpan _safetyNet;
    readonly Func<double> _random;
    bool _intact;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubscriptionCheckSchedule"/> class.
    /// </summary>
    /// <param name="revise">The interval between checks of a subscription that is not confirmed intact.</param>
    /// <param name="safetyNet">The longest interval between checks of a subscription known to be intact.</param>
    /// <param name="random">Draws a number between 0 and 1 for spreading checks; <see cref="Random.Shared"/> when not given.</param>
    internal SubscriptionCheckSchedule(TimeSpan revise, TimeSpan safetyNet, Func<double>? random = default)
    {
        Revise = revise;
        _safetyNet = safetyNet < revise ? revise : safetyNet;
        _random = random ?? Random.Shared.NextDouble;
    }

    /// <summary>
    /// Gets the interval between checks of a subscription that is not confirmed intact.
    /// </summary>
    internal TimeSpan Revise { get; }

    /// <summary>
    /// Creates the schedule configured for connected clients.
    /// </summary>
    /// <param name="options">The <see cref="ChronicleOptions"/>.</param>
    /// <returns>A new <see cref="SubscriptionCheckSchedule"/>.</returns>
    internal static SubscriptionCheckSchedule From(ChronicleOptions options)
    {
        var revise = TimeSpan.FromSeconds(options.ConnectedClients.ReviseIntervalSeconds);
        return new(
            revise < MinimumRevise ? MinimumRevise : revise,
            TimeSpan.FromSeconds(options.ConnectedClients.SubscriptionCheckIntervalSeconds));
    }

    /// <summary>
    /// Gets how long to wait before the next check.
    /// </summary>
    /// <returns>The time until the next check.</returns>
    internal TimeSpan NextCheckAfter()
    {
        if (!_intact)
        {
            return Revise;
        }

        var wait = _safetyNet * (SafetyNetFloor + ((1 - SafetyNetFloor) * _random()));
        return wait < Revise ? Revise : wait;
    }

    /// <summary>
    /// Records the outcome of a check.
    /// </summary>
    /// <param name="intact">Whether the subscription was found intact, with nothing to restore.</param>
    internal void Checked(bool intact) => _intact = intact;
}
