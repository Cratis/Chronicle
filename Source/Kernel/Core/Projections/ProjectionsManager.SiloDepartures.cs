// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Notices silos leaving the cluster, so subscriptions lost with them are reconciled.
/// </summary>
public partial class ProjectionsManager
{
    /// <summary>
    /// The in-memory cluster membership is checked every ten seconds for silos that have died. The check keeps the
    /// manager active: a manager collected while idle would stop watching, and nothing would reconcile the
    /// subscriptions lost with a silo that dies afterwards.
    /// </summary>
    internal static readonly GrainTimerCreationOptions SiloDepartureTimerOptions = new()
    {
        DueTime = TimeSpan.FromSeconds(10),
        Period = TimeSpan.FromSeconds(10),
        Interleave = false,
        KeepAlive = true
    };

    HashSet<SiloAddress> _liveSilos = [];
    IGrainTimer? _siloDepartureTimer;
    bool _reconcileAllNamespaces;

    static HashSet<SiloAddress> LiveSilos(ClusterMembershipSnapshot? snapshot) =>
        snapshot?.Members.Values.Where(member => member.Status != SiloStatus.Dead).Select(member => member.SiloAddress).ToHashSet() ?? [];

    void WatchForSiloDepartures()
    {
        _liveSilos = LiveSilos(clusterMembership.CurrentSnapshot);
        _siloDepartureTimer?.Dispose();
        _siloDepartureTimer = this.RegisterGrainTimer(
            _ =>
            {
                CheckForDepartedSilos();
                return Task.CompletedTask;
            },
            SiloDepartureTimerOptions);
    }

    /// <summary>
    /// Only a silo that is <see cref="SiloStatus.Dead"/> counts as departed. One that is shutting down still hosts
    /// the activations it has not deactivated yet, and an address seen as departed once is never seen again.
    /// </summary>
    void CheckForDepartedSilos()
    {
        var snapshot = clusterMembership.CurrentSnapshot;
        if (snapshot is null) return;

        var live = LiveSilos(snapshot);
        var departed = _liveSilos.Where(silo => !live.Contains(silo)).ToArray();
        _liveSilos = live;
        if (departed.Length == 0) return;

        logger.SilosLeftReconcilingSubscriptions(departed.Select(silo => silo.ToString()), _eventStoreName);
        _subscribed.Clear();
        _reconcileAllNamespaces = true;
        ScheduleResubscribeProjectionsThatAreNotSubscribed();
    }
}
