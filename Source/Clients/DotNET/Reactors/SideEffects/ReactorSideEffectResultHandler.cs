// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Handles a single <see cref="ReactorSideEffect"/> returned from a reactor handler method.
/// </summary>
public class ReactorSideEffectResultHandler : IReactorSideEffectHandler
{
    /// <inheritdoc/>
    public bool CanHandle(ReactorContext reactorContext, object value) => value is ReactorSideEffect;

    /// <inheritdoc/>
    public async Task Handle(ReactorContext reactorContext, IEventStore eventStore, object value)
    {
        var sideEffect = (ReactorSideEffect)value;
        await AppendSideEffect(sideEffect, reactorContext, eventStore);
    }

    /// <summary>
    /// Appends a single side effect to the appropriate event sequence.
    /// </summary>
    /// <param name="sideEffect">The <see cref="ReactorSideEffect"/> to append.</param>
    /// <param name="reactorContext">The <see cref="ReactorContext"/> for resolving defaults.</param>
    /// <param name="eventStore">The <see cref="IEventStore"/> to append to.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    internal static async Task AppendSideEffect(ReactorSideEffect sideEffect, ReactorContext reactorContext, IEventStore eventStore)
    {
        var sequence = sideEffect.EventSequenceId is not null
            ? eventStore.GetEventSequence(sideEffect.EventSequenceId)
            : eventStore.EventLog;

        await sequence.Append(
            sideEffect.EventSourceId ?? reactorContext.GetEventSourceId(),
            sideEffect.Event,
            sideEffect.EventStreamType ?? reactorContext.GetEventStreamType(),
            sideEffect.EventStreamId ?? reactorContext.GetEventStreamId(),
            sideEffect.EventSourceType ?? reactorContext.GetEventSourceType(),
            subject: sideEffect.Subject ?? reactorContext.GetSubject());
    }
}
