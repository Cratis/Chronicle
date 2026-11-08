// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_using_batch_reducers.given;

public class a_batch_reducer_scenario : Specification
{
    protected Defaults _defaults;
    protected ReadModelScenario<SeatCount> _scenario;

    void Establish()
    {
        var options = new ChronicleOptions();
        options.ExplicitArtifacts
            .RegisterEventType<SeatCounted>()
            .RegisterEventType<SeatUncounted>()
            .RegisterEventType<SeatCountBroke>()
            .RegisterEventType<SeatCountPostponed>()
            .RegisterReducer<SeatCount>();
        _defaults = new(new InMemoryClientArtifactsProvider(), null, options);
        _scenario = new(null, _defaults);
    }

    void Destroy()
    {
        _scenario.Dispose();
        ((IDisposable)_defaults.EventStore).Dispose();
    }

    [EventType]
    public record SeatCounted;

    [EventType]
    public record SeatUncounted;

    [EventType]
    public record SeatCountBroke;

    [EventType]
    public record SeatCountPostponed;

    public record SeatCount(int Seats, int Calls, ObservationOrigin Origin, int EventCount)
    {
        [Observes<SeatCounted>]
        [Observes<SeatUncounted>]
        [Observes<SeatCountBroke>]
        [Observes<SeatCountPostponed>]
        public static SeatCount? Reduce(EventBatch events, SeatCount? current, BatchContext context)
        {
            var count = current ?? new(0, 0, context.Origin, context.EventCount);
            foreach (var @event in events)
            {
                switch (@event.Event)
                {
                    case SeatCounted:
                        count = count with { Seats = count.Seats + 1 };
                        break;

                    case SeatUncounted:
                        count = count with { Seats = count.Seats - 1 };
                        break;

                    case SeatCountBroke:
                        events.Fail(@event.Context, new InvalidOperationException("The seat count broke"));
                        break;

                    case SeatCountPostponed:
                        events.Defer(@event.Context, TimeSpan.FromSeconds(30));
                        break;
                }
            }

            return count with { Calls = count.Calls + 1 };
        }
    }
}
