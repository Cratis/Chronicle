// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Monads;

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Handles a collection of event objects returned from a reactor handler method.
/// Each event is appended to the event log using metadata resolved from the <see cref="ReactorContext"/>.
/// </summary>
/// <param name="eventTypes"><see cref="IEventTypes"/> for checking whether the values are known event types.</param>
[Singleton]
public class EventsResultHandler(IEventTypes eventTypes) : IReactorSideEffectHandler
{
    /// <inheritdoc/>
    public bool CanHandle(ReactorContext reactorContext, object value) =>
        value is IEnumerable<object> events &&
        events.All(e => eventTypes.HasFor(e.GetType()));

    /// <inheritdoc/>
    public async Task<Result<ReactorSideEffectFailure>> Handle(ReactorContext reactorContext, IEventStore eventStore, object value)
    {
        var appendFailures = new List<AppendFailure>();

        foreach (var @event in (IEnumerable<object>)value)
        {
            var result = await eventStore.EventLog.Append(
                reactorContext.GetEventSourceId(),
                @event,
                reactorContext.GetEventStreamType(),
                reactorContext.GetEventStreamId(),
                reactorContext.GetEventSourceType(),
                subject: reactorContext.GetSubject());

            if (!result.IsSuccess)
            {
                var constraintViolations = result.ConstraintViolations.Select(cv =>
                    new ReactorConstraintViolation(
                        cv.EventTypeId.Value,
                        cv.Message)).ToList();

                var errors = result.Errors.Select(e => e.Value).ToList();

                var failure = new AppendFailure(
                    constraintViolations,
                    result.ConcurrencyViolation is not null,
                    errors);
                appendFailures.Add(failure);
            }
        }

        if (appendFailures.Count == 0)
        {
            return Result.Success<ReactorSideEffectFailure>();
        }

        var sideEffectFailure = new ReactorSideEffectFailure(appendFailures);
        return Result.Failed(sideEffectFailure);
    }
}
