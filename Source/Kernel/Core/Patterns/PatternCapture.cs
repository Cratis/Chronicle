// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Represents an implementation of <see cref="IPatternCapture"/>.
/// </summary>
/// <param name="storage">The <see cref="IStorage"/> to read registered event types from.</param>
/// <param name="localSiloDetails">The local silo details.</param>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> to resolve the observer with.</param>
/// <param name="logger">The <see cref="ILogger"/> for logging.</param>
/// <remarks>
/// <para>
/// Pattern capture observes every event type the event store has registered, because what it mines is the context
/// an event was appended in rather than anything about a particular event type. An observer subscribes to a list
/// of event types, so "everything" is expressed as the list of everything currently registered - and re-subscribed
/// when that list grows, which <see cref="ChronicleServerStartupTask"/> does on startup and event type
/// registration does as types arrive.
/// </para>
/// <para>
/// Capture subscribes even before the namespace holds data. Namespace creation precedes the first append, and
/// waiting for data here would leave the observer unsubscribed until another event type is registered or the
/// server restarts. <see cref="PatternCaptureSubscriptions"/> handles namespaces added after registration.
/// </para>
/// <para>
/// The observer is not replayable. Replaying it would re-mine history that is already reflected in the sketch, and
/// the sketch is a summary rather than a projection - there is no state to rebuild by starting over, only counts
/// to double.
/// </para>
/// </remarks>
[Singleton]
public class PatternCapture(
    IStorage storage,
    ILocalSiloDetails localSiloDetails,
    IGrainFactory grainFactory,
    ILogger<PatternCapture> logger) : IPatternCapture
{
    /// <summary>
    /// The identifier of the observer that captures behavior patterns.
    /// </summary>
    public const string ObserverIdentifier = "$system.patterns";

    /// <inheritdoc/>
    public async Task SubscribeAcrossNamespaces(EventStoreName eventStore)
    {
        var namespaces = await grainFactory.GetGrain<INamespaces>(eventStore).GetAll();

        foreach (var @namespace in namespaces)
        {
            try
            {
                await Subscribe(eventStore, @namespace);
            }
            catch (Exception exception)
            {
                // Registration has already persisted the types. Capture is best-effort in each namespace;
                // its event-log timer repairs missing subscriptions and type drift independently.
                logger.FailedSubscribingPatternCapture(exception, eventStore, @namespace);
            }
        }
    }

    /// <inheritdoc/>
    public Task Subscribe(EventStoreName eventStore, EventStoreNamespaceName @namespace) => Subscribe(eventStore, @namespace, recovering: false);

    /// <inheritdoc/>
    public Task RecoverSubscription(EventStoreName eventStore, EventStoreNamespaceName @namespace) => Subscribe(eventStore, @namespace, recovering: true);

    async Task Subscribe(EventStoreName eventStore, EventStoreNamespaceName @namespace, bool recovering)
    {
        // The per-type storage caches are not a complete registry. Read the authoritative set to detect
        // registrations missed by this namespace, including types it has never observed. No schemas are
        // otherwise needed here; a healthy matching subscription must not rewrite its reactor definition.
        var schemas = await storage.GetEventStore(eventStore).EventTypes.GetLatestForAllEventTypes();
        var eventTypes = schemas.Select(schema => schema.Type).ToArray();

        if (eventTypes.Length == 0)
        {
            logger.NoEventTypesToCapture(eventStore);
            return;
        }

        var key = new ObserverKey(ObserverIdentifier, eventStore, @namespace, EventSequenceId.Log);
        var observer = grainFactory.GetGrain<IObserver>(key);
        if (recovering && !await observer.NeedsSubscriptionRecovery(eventTypes))
        {
            return;
        }

        logger.SubscribingPatternCapture(eventStore, @namespace, eventTypes.Length);

        await storage.GetEventStore(eventStore).Reactors.Save(new ReactorDefinition(
            key.ObserverId,
            ReactorOwner.Kernel,
            EventSequenceId.Log,
            [.. eventTypes.Select(eventType => new EventTypeWithKeyExpression(eventType, WellKnownExpressions.EventSourceId))],
            false));

        if (recovering)
        {
            await observer.RecoverStalledSubscription<IPatternCaptureSubscriber>(
                ObserverType.Reactor, eventTypes, localSiloDetails.SiloAddress, isReplayable: false);
        }
        else
        {
            await observer.Subscribe<IPatternCaptureSubscriber>(
                ObserverType.Reactor, eventTypes, localSiloDetails.SiloAddress, isReplayable: false);
        }
    }
}
