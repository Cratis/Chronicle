// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IEventSequences"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventSequences"/> class.
/// </remarks>
/// <param name="logger">Logger for logging.</param>
public class EventSequences(ILogger<EventSequences> logger) : Grain, IEventSequences
{
    EventSequencesKey _key = EventSequencesKey.NotSet;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _ = this.GetPrimaryKeyLong(out var keyAsString);
        _key = keyAsString;

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Rehydrate()
    {
        var eventSequences = new[]
        {
            EventSequenceId.Log,
            EventSequenceId.System,
        };

        var eventSequenceKey = new EventSequenceKey(_key.EventStore, _key.Namespace);
        foreach (var eventSequence in eventSequences)
        {
            var grain = GrainFactory.GetGrain<IEventSequence>(eventSequence, eventSequenceKey);
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
