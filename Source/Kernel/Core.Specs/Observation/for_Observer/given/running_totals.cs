// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class running_totals : Specification
{
    protected static readonly Func<ObserverState, IReadOnlyDictionary<EventTypeId, EventCount>, ObserverState> Increment =
        typeof(Observer).GetMethod("WithIncrementedRunningTotals", BindingFlags.Static | BindingFlags.NonPublic)!
            .CreateDelegate<Func<ObserverState, IReadOnlyDictionary<EventTypeId, EventCount>, ObserverState>>();

    protected static readonly Func<ObserverState, Dictionary<EventTypeId, EventCount>, ObserverState> Subtract =
        typeof(Observer).GetMethod("WithSubtractedPartitionHandledEventCounts", BindingFlags.Static | BindingFlags.NonPublic)!
            .CreateDelegate<Func<ObserverState, Dictionary<EventTypeId, EventCount>, ObserverState>>();

    protected ObserverState _state;
    protected ObserverState _result;

    void Establish() => _state = ObserverState.Empty with
    {
        HandledEventCount = 8UL,
        HandledEventCountPerEventType = new Dictionary<EventTypeId, EventCount> { ["first"] = 3UL, ["second"] = 5UL }
    };
}
