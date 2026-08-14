// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IEventSequences"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventSequences"/> class.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for getting the sequences the namespace holds.</param>
/// <param name="logger">Logger for logging.</param>
public class EventSequences(IStorage storage, ILogger<EventSequences> logger) : Grain, IEventSequences
{
    /// <summary>
    /// The sequences every namespace offers, whether or not anything has been appended to them.
    /// </summary>
    static readonly EventSequenceId[] _wellKnown =
    [
        EventSequenceId.Log,
        EventSequenceId.System,
        EventSequenceId.Outbox
    ];

    EventSequencesKey _key = EventSequencesKey.NotSet;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _ = this.GetPrimaryKeyLong(out var keyAsString);
        _key = keyAsString!;

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventSequenceId>> GetEventSequences()
    {
        var stored = await storage.GetEventStore(_key.EventStore).GetNamespace(_key.Namespace).GetEventSequences();

        return [.. _wellKnown.Concat(stored).Distinct().OrderBy(_ => _.Value)];
    }

    /// <inheritdoc/>
    public async Task Rehydrate()
    {
        foreach (var eventSequence in await GetEventSequences())
        {
            var eventSequenceKey = new EventSequenceKey(eventSequence, _key.EventStore, _key.Namespace);
            var grain = GrainFactory.GetGrain<IEventSequence>(eventSequenceKey);
            try
            {
                await grain.Rehydrate();
            }
            catch (Exception ex)
            {
                logger.FailedRehydratingEventSequence(eventSequence, _key.EventStore, _key.Namespace, ex);
            }
        }
    }
}
