// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Monads;

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Handles a single event object returned from a reactor handler method.
/// The event is appended to the event log using metadata resolved from the <see cref="ReactorContext"/>.
/// </summary>
/// <param name="eventTypes"><see cref="IEventTypes"/> for checking whether the value is a known event type.</param>
[Singleton]
public class EventResultHandler(IEventTypes eventTypes) : IReactorSideEffectHandler
{
    /// <inheritdoc/>
    public bool CanHandle(ReactorContext reactorContext, object value) =>
        eventTypes.HasFor(value.GetType());

    /// <inheritdoc/>
    public async Task<Result<ReactorSideEffectFailure>> Handle(ReactorContext reactorContext, IEventStore eventStore, object value)
    {
        var result = await eventStore.EventLog.Append(
            reactorContext.GetEventSourceId(),
            value,
            reactorContext.GetEventStreamType(),
            reactorContext.GetEventStreamId(),
            reactorContext.GetEventSourceType(),
            subject: reactorContext.GetSubject());

        if (result.IsSuccess)
        {
            return Result.Success<ReactorSideEffectFailure>();
        }

        var constraintViolations = result.ConstraintViolations.Select(cv =>
            new ReactorConstraintViolation(
                cv.EventTypeId.Value,
                cv.Message)).ToList();

        var errors = result.Errors.Select(e => e.Value).ToList();

        var failure = new ReactorSideEffectFailure(
            [new AppendFailure(constraintViolations, result.ConcurrencyViolation is not null, errors)]);

        return Result.Failed(failure);
    }
}
