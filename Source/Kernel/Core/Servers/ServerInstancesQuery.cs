// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Servers;

/// <summary>
/// Represents the cluster-wide view of running server instances and their live resource usage.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> to get grains with.</param>
/// <param name="options"><see cref="IOptions{ChronicleOptions}"/> for configuration.</param>
/// <remarks>
/// This is a live snapshot only - no history is persisted. Every call re-asks the cluster and every
/// silo's process for current numbers, there is nothing to replay or chart over time yet.
/// </remarks>
public sealed class ServerInstancesQuery(IGrainFactory grainFactory, IOptions<ChronicleOptions> options)
{
    readonly TimeSpan _observeInterval = TimeSpan.FromSeconds(options.Value.Servers.ObserveIntervalSeconds);

    /// <summary>
    /// Gets every running server instance (silo) in the cluster, with a live snapshot of its CPU and memory usage.
    /// </summary>
    /// <returns>A collection of <see cref="ServerInstanceDetails"/>.</returns>
    /// <remarks>
    /// The per-silo lookups are issued together, so a faulting silo no longer aborts the sweep before the
    /// remaining silos are asked - every silo is queried, and the first fault surfaces once they have all
    /// settled rather than immediately. A fault still fails the whole call rather than returning a partial
    /// cluster view.
    /// </remarks>
    public async Task<IEnumerable<ServerInstanceDetails>> GetAll()
    {
        var management = grainFactory.GetGrain<IManagementGrain>(0);
        var hosts = await management.GetHosts(onlyActive: true);
        return await Task.WhenAll(hosts.Select(host => GetInstance(host.Key, host.Value)));
    }

    /// <summary>
    /// Observes every running server instance in the cluster, polling for fresh CPU and memory snapshots at
    /// the configured interval.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for cancelling the observation.</param>
    /// <returns>An observable of collections of <see cref="ServerInstanceDetails"/>.</returns>
    public IObservable<IEnumerable<ServerInstanceDetails>> ObserveAll(CancellationToken cancellationToken)
    {
        var subject = new Subject<IEnumerable<ServerInstanceDetails>>();
        var subscription = Observable
            .Timer(TimeSpan.Zero, _observeInterval)
            .SelectMany(_ => Observable.FromAsync(GetAll))
            .DistinctUntilChanged(ServerInstancesComparer.Instance)
            .Subscribe(subject);

        cancellationToken.Register(() =>
        {
            subscription.Dispose();
            subject.OnCompleted();
        });

        return subject;
    }

    async Task<ServerInstanceDetails> GetInstance(SiloAddress silo, SiloStatus status)
    {
        var metrics = await grainFactory.GetServerInstance(silo).GetMetrics();
        var address = silo.ToParsableString();
        return new(address, address, status.ToString(), metrics.CpuUsagePercentage, metrics.WorkingSetBytes);
    }

    sealed class ServerInstancesComparer : IEqualityComparer<IEnumerable<ServerInstanceDetails>>
    {
        public static readonly ServerInstancesComparer Instance = new();

        public bool Equals(IEnumerable<ServerInstanceDetails>? x, IEnumerable<ServerInstanceDetails>? y)
        {
            if (x is null || y is null)
            {
                return ReferenceEquals(x, y);
            }

            return x.Select(Identity).Order().SequenceEqual(y.Select(Identity).Order());
        }

        public int GetHashCode(IEnumerable<ServerInstanceDetails> obj) => 0;

        /// <summary>
        /// Gets the identity of a server instance, covering every value observers display - including the
        /// live CPU/memory numbers, so a watching Workbench sees them tick on every poll. The comparer's
        /// suppression then only kicks in when a poll produces an identical snapshot, which in practice
        /// means an idle cluster.
        /// </summary>
        /// <param name="instance">The <see cref="ServerInstanceDetails"/> to get the identity of.</param>
        /// <returns>The identity as a string.</returns>
        static string Identity(ServerInstanceDetails instance) =>
            $"{instance.Address}/{instance.Status}/{instance.CpuUsagePercentage:F2}/{instance.MemoryUsageBytes}";
    }
}
