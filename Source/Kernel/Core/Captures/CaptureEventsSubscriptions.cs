// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Captures.Engine;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Captures;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Represents an implementation of <see cref="ICaptureEventsSubscriptions"/>.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for resolving the event types to observe and removing what a capture remembers.</param>
/// <param name="localSiloDetails"><see cref="ILocalSiloDetails"/> for the silo the subscriber runs on.</param>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for getting the observers and namespaces.</param>
/// <param name="logger">The logger.</param>
[Singleton]
public class CaptureEventsSubscriptions(
    IStorage storage,
    ILocalSiloDetails localSiloDetails,
    IGrainFactory grainFactory,
    ILogger<CaptureEventsSubscriptions> logger) : ICaptureEventsSubscriptions
{
    /// <inheritdoc/>
    public async Task Subscribe(EventStoreName eventStore, CaptureDefinition definition)
    {
        var (sequence, eventTypes) = await Resolve(eventStore, definition);
        var subscribed = new List<EventStoreNamespaceName>();

        // A capture that only runs in some namespaces must not report that it started, so a failure undoes
        // the namespaces already subscribed instead of leaving the capture half running.
        try
        {
            foreach (var @namespace in await GetNamespaces(eventStore))
            {
                await SubscribeIn(eventStore, @namespace, definition, sequence, eventTypes);
                subscribed.Add(@namespace);
            }
        }
        catch
        {
            foreach (var @namespace in subscribed)
            {
                try
                {
                    await GetObserver(eventStore, @namespace, definition.Id, sequence).Unsubscribe();
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.FailedRollingBackCaptureSubscription(exception, definition.Name, @namespace);
                }
            }

            throw;
        }
    }

    /// <inheritdoc/>
    public async Task Subscribe(EventStoreName eventStore, EventStoreNamespaceName @namespace, CaptureDefinition definition)
    {
        var (sequence, eventTypes) = await Resolve(eventStore, definition);
        await SubscribeIn(eventStore, @namespace, definition, sequence, eventTypes);
    }

    /// <inheritdoc/>
    public async Task Recover(EventStoreName eventStore, CaptureDefinition definition)
    {
        var (sequence, eventTypes) = await Resolve(eventStore, definition);
        var failures = new List<Exception>();

        foreach (var @namespace in await GetNamespaces(eventStore))
        {
            try
            {
                var observer = GetObserver(eventStore, @namespace, definition.Id, sequence);
                if (!await observer.NeedsSubscriptionRecovery(eventTypes))
                {
                    continue;
                }

                logger.RecoveringCaptureSubscription(definition.Name, eventStore, @namespace);
                await observer.RecoverStalledSubscription<ICaptureEventsSubscriber>(
                    ObserverType.Reactor,
                    eventTypes,
                    localSiloDetails.SiloAddress,
                    isReplayable: false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                failures.Add(exception);
            }
        }

        if (failures.Count > 0)
        {
            throw new AggregateException($"The capture '{definition.Name}' could not be recovered in every namespace", failures);
        }
    }

    /// <inheritdoc/>
    public async Task Unsubscribe(EventStoreName eventStore, CaptureDefinition definition)
    {
        var sequence = await ResolveForRemoval(eventStore, definition);
        if (sequence is null)
        {
            return;
        }

        foreach (var @namespace in await GetNamespaces(eventStore))
        {
            await GetObserver(eventStore, @namespace, definition.Id, sequence).Unsubscribe();
        }
    }

    /// <inheritdoc/>
    public async Task Remove(EventStoreName eventStore, CaptureDefinition definition)
    {
        var sequence = await ResolveForRemoval(eventStore, definition);
        var eventStoreStorage = storage.GetEventStore(eventStore);
        foreach (var @namespace in await GetNamespaces(eventStore))
        {
            if (sequence is not null)
            {
                var observer = GetObserver(eventStore, @namespace, definition.Id, sequence);
                await observer.Unsubscribe();
                await observer.Remove();
            }

            // The captured state of this namespace - every namespace keeps its own.
            await eventStoreStorage.Captures.Delete(CaptureObservers.ObservationIdFor(definition.Id, @namespace));
        }

        await eventStoreStorage.Observers.Delete(CaptureObservers.For(definition.Id));
    }

    static void ThrowWhenNotEvents(CaptureDefinition definition)
    {
        if (definition.Source.Type != SourceType.Events)
        {
            throw new UnsupportedCaptureCapability($"The capture '{definition.Name}' does not have an events source");
        }
    }

    async Task<(string Sequence, EventType[] EventTypes)> Resolve(EventStoreName eventStore, CaptureDefinition definition)
    {
        ThrowWhenNotEvents(definition);
        var resolved = await EventsCaptureSequence.ResolveFor(storage.GetEventStore(eventStore).EventTypes, definition.Source);
        if (resolved.Errors.Count > 0 || string.IsNullOrWhiteSpace(resolved.Sequence))
        {
            throw new UnsupportedCaptureCapability($"The capture '{definition.Name}' cannot read from an inbox: {string.Join("; ", resolved.Errors)}");
        }

        return (resolved.Sequence, [.. resolved.Schemas.Select(schema => schema.Type)]);
    }

    async Task<string?> ResolveForRemoval(EventStoreName eventStore, CaptureDefinition definition)
    {
        ThrowWhenNotEvents(definition);
        if (!string.IsNullOrWhiteSpace(definition.Source.Sequence))
        {
            return definition.Source.Sequence;
        }

        var resolved = await EventsCaptureSequence.ResolveFor(storage.GetEventStore(eventStore).EventTypes, definition.Source);
        if (string.IsNullOrWhiteSpace(resolved.Sequence))
        {
            logger.CouldNotResolveCaptureSequence(definition.Name);
            return null;
        }

        return resolved.Sequence;
    }

    async Task SubscribeIn(EventStoreName eventStore, EventStoreNamespaceName @namespace, CaptureDefinition definition, string sequence, EventType[] eventTypes) =>

        // Additive, so a registration of a newer generation that landed in between is never narrowed away.
        await GetObserver(eventStore, @namespace, definition.Id, sequence).SubscribeAdditively<ICaptureEventsSubscriber>(
            ObserverType.Reactor,
            eventTypes,
            localSiloDetails.SiloAddress,
            isReplayable: false);

    async Task<IEnumerable<EventStoreNamespaceName>> GetNamespaces(EventStoreName eventStore) =>
        await grainFactory.GetGrain<INamespaces>(eventStore).GetAll();

    IObserver GetObserver(EventStoreName eventStore, EventStoreNamespaceName @namespace, CaptureId captureId, string sequence) =>
        grainFactory.GetGrain<IObserver>(new ObserverKey(CaptureObservers.For(captureId), eventStore, @namespace, new EventSequenceId(sequence)));
}
