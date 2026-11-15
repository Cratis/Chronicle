// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Storage;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Appends federated events to the inbox the same way event store subscriptions do, and finds the resume point from the inbox tail.
/// </summary>
/// <param name="grainFactory">The grain factory.</param>
/// <param name="storage">The storage.</param>
/// <param name="namespaceCasing">The namespace casing.</param>
/// <param name="timeProvider">The time provider.</param>
[Singleton]
public class FederationInbox(
    IGrainFactory grainFactory,
    IStorage storage,
    INamespaceCasing namespaceCasing,
    TimeProvider? timeProvider = null) : IFederationInbox
{
    /// <summary>
    /// The inbox may hold events forwarded by other means; look this far back for the last federated one.
    /// </summary>
    const int MaxLookBack = 16;

    readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    /// <inheritdoc/>
    public async Task<FederationResumePoint?> GetResumePoint(FederationBindingState binding)
    {
        var (targetEventStore, targetNamespace) = await Resolve(binding);
        var sequence = storage.GetEventStore(targetEventStore).GetNamespace(targetNamespace).GetEventSequence(InboxId(binding));
        var tail = await sequence.GetTailSequenceNumber();
        for (var step = 0; step < MaxLookBack && tail.IsActualValue; step++)
        {
            var resumePoint = FederationCausation.Read((await sequence.GetEventAt(tail)).Context.Causation);
            if (resumePoint is not null)
            {
                return resumePoint;
            }

            if (tail == EventSequenceNumber.First)
            {
                break;
            }

            tail -= 1;
        }

        return null;
    }

    /// <inheritdoc/>
    public async Task<string?> Append(FederationBindingState binding, string originEventStore, FederatedEvent @event)
    {
        var (targetEventStore, targetNamespace) = await Resolve(binding);
        var inbox = grainFactory.GetEventSequence(InboxId(binding), targetEventStore, targetNamespace);

        var content = JsonNode.Parse(@event.Content) as JsonObject ?? [];
        var result = await inbox.Append(
            new EventSourceType(@event.EventSourceType),
            new EventSourceId(@event.EventSourceId),
            new EventStreamType(@event.EventStreamType),
            new EventStreamId(@event.EventStreamId),
            new EventType(new EventTypeId(@event.EventType.Split('+')[0]), new EventTypeGeneration(@event.Generation)),
            content,
            new CorrelationId(Guid.TryParse(@event.CorrelationId, out var correlationId) ? correlationId : Guid.NewGuid()),
            [FederationCausation.Create(originEventStore, @event.SequenceNumber, binding.LogicalSource, _timeProvider.GetUtcNow())],
            Identity.System,
            [.. @event.Tags.Select(_ => new Tag(_))],
            ConcurrencyScope.None,
            @event.Occurred,
            new Subject(@event.Subject));

        if (result.IsSuccess)
        {
            return null;
        }

        return result switch
        {
            { HasConstraintViolations: true } => "Inbox append constraint violation",
            { HasConcurrencyViolations: true } => "Inbox append concurrency violation",
            _ => "Inbox append error"
        };
    }

    static EventSequenceId InboxId(FederationBindingState binding) => new($"{EventSequenceId.InboxPrefix}{binding.LogicalSource}");

    async Task<(EventStoreName EventStore, EventStoreNamespaceName Namespace)> Resolve(FederationBindingState binding)
    {
        var eventStore = new EventStoreName(binding.TargetEventStore);
        return (eventStore, await namespaceCasing.Canonicalize(eventStore, new EventStoreNamespaceName(binding.TargetNamespace)));
    }
}
