// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Persistence.EventSequences;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IEventSequences"/>.
/// </summary>
public class EventSequences : Grain, IEventSequences
{
    readonly IExecutionContextManager _executionContextManager;
    readonly ILogger<EventSequences> _logger;
    EventSequencesKey _key = EventSequencesKey.NotSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequences"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for managing the execution context.</param>
    /// <param name="logger">Logger for logging.</param>
    public EventSequences(
        IExecutionContextManager executionContextManager,
        ILogger<EventSequences> logger)
    {
        _executionContextManager = executionContextManager;
        _logger = logger;
    }

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

        var eventSequenceKey = new EventSequenceKey(_key.MicroserviceId, _key.TenantId);
        foreach (var eventSequence in eventSequences)
        {
            var grain = GrainFactory.GetGrain<IEventSequence>(eventSequence, eventSequenceKey);
            try
            {
                _executionContextManager.Establish(_key.TenantId, CorrelationId.New(), _key.MicroserviceId);
                await grain.Rehydrate();
            }
            catch (Exception ex)
            {
                _logger.FailedRehydratingEventSequence(eventSequence, _key.MicroserviceId, _key.TenantId, ex);
            }
        }
    }
}
