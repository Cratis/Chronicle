// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverTypeSetControlUpdate.when_guarding;

public class with_other_update_operators : Specification
{
    ObserverTypeSetControl _control;
    Exception?[] _errors;

    void Establish() => _control = new(new(1), new("reactor-subscriber", Concepts.Observation.ObserverFilters.None), new Dictionary<EventTypeId, ObserverTypeSetMember>(), []);

    void Because()
    {
        var set = Builders<ObserverState>.Update.Set(state => state.NextEventSequenceNumber, new EventSequenceNumber(42));
        UpdateDefinition<ObserverState>[] updates =
        [
            set.Inc(state => state.FailedPartitionCount, 1),
            set.Unset(state => state.Initialization),
            set.SetOnInsert(state => state.Id, new ObserverId("observer")),
            Builders<ObserverState>.Update.Inc(state => state.FailedPartitionCount, 1)
        ];
        _errors = updates.Select(update => Catch.Exception(() => ObserverTypeSetControlUpdate.Guard(update, _control))).ToArray();
    }

    [Fact] void should_refuse_every_update_that_would_lose_an_operator() => _errors.ShouldEachConformTo(error => error is UnsupportedObserverStateUpdateOperator);
}
