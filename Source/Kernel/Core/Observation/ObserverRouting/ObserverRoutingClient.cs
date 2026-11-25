// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Diagnostics;
using Cratis.Chronicle.Storage.Observation;
using Orleans.Runtime.Services;

namespace Cratis.Chronicle.Observation.ObserverRouting;

/// <summary>
/// Represents an implementation of <see cref="IObserverRoutingClient"/>.
/// </summary>
/// <remarks>
/// The silos are read from the local cluster membership snapshot, so a publication makes no grain call to find them.
/// </remarks>
/// <param name="membership">The <see cref="IClusterMembershipService"/> that knows the active silos.</param>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/> used to reach the grain service of a silo.</param>
public class ObserverRoutingClient(IClusterMembershipService membership, IServiceProvider serviceProvider)
    : GrainServiceClient<IObserverRoutingGrainService>(serviceProvider), IObserverRoutingClient
{
    /// <inheritdoc/>
    public async Task<ObserverRevisionAcknowledgements> Publish(EventStoreName eventStore, ObserverId observerId, ObserverDefinitionRevision revision)
    {
        var silos = membership.CurrentSnapshot.Members.Values
            .Where(_ => _.Status == SiloStatus.Active)
            .Select(_ => _.SiloAddress)
            .ToArray();
        var answers = await Task.WhenAll(silos.Select(silo => Ask(silo, eventStore, observerId, revision)));
        return ObserverRevisionAcknowledgementsTally.Tally(revision, silos.Zip(answers));
    }

    async Task<ObserverDefinitionRevision?> Ask(SiloAddress silo, EventStoreName eventStore, ObserverId observerId, ObserverDefinitionRevision revision)
    {
        try
        {
            return await GetGrainService(silo).Acknowledge(eventStore, observerId, revision);
        }
        catch (Exception exception) when (ObservationOnlyFailures.CanBeContained(exception))
        {
            // A silo of an earlier release has no such service; it is counted as lagging and catches up on its own.
            return null;
        }
    }
}
