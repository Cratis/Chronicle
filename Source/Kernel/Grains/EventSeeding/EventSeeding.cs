// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.EventSeeding;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Grains.EventSequences;
using Cratis.Chronicle.Storage.EventSeeding;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Cratis.Chronicle.Grains.EventSeeding;

/// <summary>
/// Represents an implementation of <see cref="IEventSeeding"/>.
/// </summary>
/// <param name="state">The <see cref="IPersistentState{T}"/> for the seeding data.</param>
/// <param name="logger">The <see cref="ILogger"/> for logging.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.EventSeeding)]
public class EventSeeding(
    [PersistentState(nameof(EventSeedingData), WellKnownGrainStorageProviders.EventSeeding)]
    IPersistentState<EventSeedingData> state,
    ILogger<EventSeeding> logger) : Grain, IEventSeeding
{
    EventSeedingKey _key = EventSeedingKey.NotSet;
    IEventSequence _eventSequence = null!;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _key = EventSeedingKey.Parse(this.GetPrimaryKeyString());
        _eventSequence = GrainFactory.GetGrain<IEventSequence>(
            new EventSequenceKey(EventSequenceId.Log, _key.EventStore, _key.Namespace));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Seed(IEnumerable<SeedingEntry> entries)
    {
        logger.LogInformation("Seeding events for event store '{EventStore}' in namespace '{Namespace}'", _key.EventStore, _key.Namespace);

        var entriesList = entries.ToList();
        if (entriesList.Count == 0)
        {
            return;
        }

        // Initialize storage if needed
        if (state.State == null!)
        {
            state.State = new EventSeedingData(
                _key.EventStore,
                _key.Namespace,
                new Dictionary<EventTypeId, IList<SeededEventEntry>>(),
                new Dictionary<EventSourceId, IList<SeededEventEntry>>());
        }

        // Collect new events that haven't been seeded yet
        var eventsToAppend = new List<EventToAppend>();

        foreach (var entry in entriesList)
        {
            var seededEntry = new SeededEventEntry(entry.EventSourceId, entry.EventTypeId, entry.Content);

            // Check if this exact event has already been seeded
            var alreadySeeded = IsAlreadySeeded(seededEntry);

            if (!alreadySeeded)
            {
                // Add to storage tracking
                TrackSeededEvent(seededEntry);

                // Prepare for appending
                var content = JsonSerializer.Deserialize<JsonObject>(entry.Content)!;
                eventsToAppend.Add(new EventToAppend(
                    string.Empty, // EventSourceType - empty for seeded events
                    entry.EventSourceId,
                    EventStreamType.All,
                    EventStreamId.Default,
                    new EventType((EventTypeId)entry.EventTypeId, 1), // EventType with generation 1
                    content));
            }
        }

        // Append all new events in one operation if there are any
        if (eventsToAppend.Count > 0)
        {
            logger.LogInformation("Appending {Count} new seeded events", eventsToAppend.Count);

            await _eventSequence.AppendMany(
                eventsToAppend,
                CorrelationId.New(),
                [new Causation(DateTimeOffset.UtcNow, "event-seeding", new Dictionary<string, string>())],
                Identity.System,
                new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));

            // Save state
            await state.WriteStateAsync();
        }
        else
        {
            logger.LogInformation("All events have already been seeded, skipping");
        }
    }

    bool IsAlreadySeeded(SeededEventEntry entry)
    {
        // Check in ByEventType
        if (state.State.ByEventType.TryGetValue(entry.EventTypeId, out var byTypeEntries))
        {
            if (byTypeEntries.Any(e => e.EventSourceId == entry.EventSourceId &&
                                      e.EventTypeId == entry.EventTypeId &&
                                      e.Content == entry.Content))
            {
                return true;
            }
        }

        // Check in ByEventSource
        if (state.State.ByEventSource.TryGetValue(entry.EventSourceId, out var bySourceEntries))
        {
            if (bySourceEntries.Any(e => e.EventSourceId == entry.EventSourceId &&
                                        e.EventTypeId == entry.EventTypeId &&
                                        e.Content == entry.Content))
            {
                return true;
            }
        }

        return false;
    }

    void TrackSeededEvent(SeededEventEntry entry)
    {
        // Track by event type
        if (!state.State.ByEventType.ContainsKey(entry.EventTypeId))
        {
            state.State.ByEventType[entry.EventTypeId] = new List<SeededEventEntry>();
        }
        state.State.ByEventType[entry.EventTypeId].Add(entry);

        // Track by event source
        if (!state.State.ByEventSource.ContainsKey(entry.EventSourceId))
        {
            state.State.ByEventSource[entry.EventSourceId] = new List<SeededEventEntry>();
        }
        state.State.ByEventSource[entry.EventSourceId].Add(entry);
    }
}
