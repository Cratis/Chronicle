// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Queries;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Discovery;

/// <summary>
/// Compares discovered artifacts with stored identities before registration.
/// </summary>
/// <param name="servicesAccessor">The kernel services accessor.</param>
/// <param name="eventStore">The event store to inspect.</param>
/// <param name="namespace">The namespace to inspect.</param>
/// <param name="eventTypes">The discovered event types.</param>
/// <param name="logger">The diagnostic logger.</param>
internal class ArtifactIdentityDriftDetector(
    IChronicleServicesAccessor servicesAccessor,
    EventStoreName eventStore,
    EventStoreNamespaceName @namespace,
    Events.IEventTypes eventTypes,
    ILogger logger)
{
    /// <summary>
    /// Detects likely identity changes without failing on unavailable diagnostics.
    /// </summary>
    /// <param name="current">The current artifacts.</param>
    /// <returns>The findings, with reactor and reducer candidates confirmed unsubscribed.</returns>
    internal async Task<IImmutableList<ArtifactIdentityDrift>> Detect(DiscoveryReport current)
    {
        try
        {
            // This diagnostic must not hold startup indefinitely when the kernel is slow.
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var observers = (await servicesAccessor.Services.Observers.GetObservers(
                new AllObserversRequest { EventStore = eventStore, Namespace = @namespace }, deadline.Token)).ToArray();
            var stored = await servicesAccessor.Services.EventTypes.AllEventTypes(
                new AllEventTypesRequest { EventStore = eventStore }, deadline.Token).EnsureSuccess();
            var drift = ImmutableList.CreateBuilder<ArtifactIdentityDrift>();
            drift.AddRange(EventTypeIdentityDrift.Find(stored, current, id => eventTypes.GetSchemaFor(id).ToJson()));

            foreach (var candidate in ObserverIdentityDrift.Find(observers, current))
            {
                var observer = observers.First(_ => _.Id == candidate.PreviousId);

                // Kernel-owned projections remain subscribed until registration retires their old identities.
                if (candidate.Kind == ArtifactKind.Projection || await IsUnsubscribed(observer, deadline.Token))
                {
                    drift.Add(candidate);
                }
            }

            foreach (var finding in drift)
            {
                Log(finding);
            }

            return drift.ToImmutable();
        }
        catch (Exception exception)
        {
            logger.IdentityDriftCheckSkipped(exception, eventStore, @namespace);
            return ImmutableList<ArtifactIdentityDrift>.Empty;
        }
    }

    async Task<bool> IsUnsubscribed(ObserverInformation observer, CancellationToken cancellationToken)
    {
        try
        {
            var information = await servicesAccessor.Services.Observers.GetObserverInformation(
                new GetObserverInformationRequest
                {
                    EventStore = eventStore,
                    Namespace = @namespace,
                    ObserverId = observer.Id,
                    EventSequenceId = observer.EventSequenceId
                },
                cancellationToken);

            return !information.IsSubscribed;
        }
        catch (Exception exception)
        {
            logger.IdentityDriftConfirmationFailed(exception, observer.Id);
            return false;
        }
    }

    void Log(ArtifactIdentityDrift drift)
    {
        switch (drift.Kind)
        {
            case ArtifactKind.Reactor:
                logger.ReactorLikelyRenamed(drift.PreviousId, drift.CurrentId, eventStore, @namespace);
                break;
            case ArtifactKind.EventType:
                logger.EventTypeLikelyRenamed(drift.PreviousId, drift.CurrentId, eventStore);
                break;
            default:
                logger.ObserverLikelyRenamed(drift.Kind, drift.PreviousId, drift.CurrentId, eventStore, @namespace);
                break;
        }
    }
}
