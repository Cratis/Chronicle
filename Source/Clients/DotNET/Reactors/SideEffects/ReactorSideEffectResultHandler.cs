// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Handles a single <see cref="ReactorSideEffect"/> returned from a reactor handler method.
/// </summary>
public class ReactorSideEffectResultHandler : IReactorSideEffectHandler
{
    /// <inheritdoc/>
    public bool CanHandle(EventContext eventContext, object value) => value is ReactorSideEffect;

    /// <inheritdoc/>
    public async Task Handle(EventContext eventContext, IEventStore eventStore, object value)
    {
        var sideEffect = (ReactorSideEffect)value;
        await AppendSideEffect(sideEffect, eventContext, eventStore);
    }

    /// <summary>
    /// Appends a single side effect to the appropriate event sequence.
    /// </summary>
    /// <param name="sideEffect">The <see cref="ReactorSideEffect"/> to append.</param>
    /// <param name="eventContext">The <see cref="EventContext"/> of the triggering event.</param>
    /// <param name="eventStore">The <see cref="IEventStore"/> to append to.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    internal static async Task AppendSideEffect(ReactorSideEffect sideEffect, EventContext eventContext, IEventStore eventStore)
    {
        var sequence = sideEffect.EventSequenceId is not null
            ? eventStore.GetEventSequence(sideEffect.EventSequenceId)
            : eventStore.EventLog;

        await sequence.Append(
            sideEffect.EventSourceId ?? eventContext.EventSourceId,
            sideEffect.Event,
            sideEffect.EventStreamType,
            sideEffect.EventStreamId,
            sideEffect.EventSourceType,
            subject: sideEffect.Subject);
    }
}
