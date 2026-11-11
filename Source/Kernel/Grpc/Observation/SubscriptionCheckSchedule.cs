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
/// check failed - is checked every revise interval, so a restore is confirmed or retried quickly. So is every
/// subscription for a settle window after a silo leaves the cluster, which is what loses subscriptions. Otherwise a
/// subscription known to be intact is only checked every safety-net interval.
/// </para>
/// <para>
/// A lost subscription is therefore restored within the revise interval after it is first checked: within the settle
/// window when it was lost with a silo, and within the safety-net interval when it was lost any other way.
/// </para>
/// </remarks>
/// <param name="revise">The interval between checks of a subscription that is not confirmed intact.</param>
/// <param name="safetyNet">The interval between checks of a subscription known to be intact.</param>
/// <param name="settle">How long after a silo leaves every subscription is checked every <paramref name="revise"/> interval.</param>
public sealed class SubscriptionCheckSchedule(TimeSpan revise, TimeSpan safetyNet, TimeSpan settle)
{
    readonly TimeSpan _safetyNet = safetyNet < revise ? revise : safetyNet;
    bool _intact;
    DateTimeOffset _settlingUntil = DateTimeOffset.MinValue;

    /// <summary>
    /// Gets the interval between checks of a subscription that is not confirmed intact.
    /// </summary>
    public TimeSpan Revise => revise;

    /// <summary>
    /// Creates the schedule configured for connected clients.
    /// </summary>
    /// <param name="options">The <see cref="ChronicleOptions"/>.</param>
    /// <returns>A new <see cref="SubscriptionCheckSchedule"/>.</returns>
    public static SubscriptionCheckSchedule From(ChronicleOptions options) => new(
        TimeSpan.FromSeconds(options.ConnectedClients.ReviseIntervalSeconds),
        TimeSpan.FromSeconds(options.ConnectedClients.SubscriptionCheckIntervalSeconds),
        TimeSpan.FromSeconds(options.ConnectedClients.SubscriptionSettleSeconds));

    /// <summary>
    /// Gets how long to wait before the next check.
    /// </summary>
    /// <param name="now">The current time.</param>
    /// <returns>The time until the next check.</returns>
    public TimeSpan NextCheckAfter(DateTimeOffset now) => !_intact || now < _settlingUntil ? revise : _safetyNet;

    /// <summary>
    /// Records the outcome of a check.
    /// </summary>
    /// <param name="intact">Whether the subscription was found intact, with nothing to restore.</param>
    public void Checked(bool intact) => _intact = intact;

    /// <summary>
    /// Records that a silo left the cluster.
    /// </summary>
    /// <param name="now">The current time.</param>
    public void SiloLeft(DateTimeOffset now) => _settlingUntil = now + settle;
}
