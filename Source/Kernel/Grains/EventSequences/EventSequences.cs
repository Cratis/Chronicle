// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;

namespace Aksio.Cratis.Kernel.Grains.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IEventSequences"/>.
/// </summary>
public class EventSequences : Grain, IEventSequences
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
            EventSequenceId.Inbox,
            EventSequenceId.Outbox,
            EventSequenceId.System,
        };

        var eventSequenceKey = new MicroserviceAndTenant(_key.MicroserviceId, _key.TenantId);
        foreach (var eventSequence in eventSequences)
        {
            var grain = GrainFactory.GetGrain<IEventSequence>(eventSequence, eventSequenceKey);
            await grain.Rehydrate();
        }
    }
}
