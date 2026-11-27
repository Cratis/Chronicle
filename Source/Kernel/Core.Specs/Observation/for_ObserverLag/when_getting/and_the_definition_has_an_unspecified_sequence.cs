// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverLag.when_getting;

public class and_the_definition_has_an_unspecified_sequence : given.an_observer_lag_query
{
    ObserverLag _result;

    void Establish()
    {
        var definition = Definition with { EventSequenceId = EventSequenceId.Unspecified };
        _storage.GetEventStore(Store).Observers.Get(Id).Returns(definition);
        var sample = new ObserverLagSample(ObserverLagStatus.Lagging, ObserverLagCause.Unknown, 12UL, 5UL, 4UL, 5UL, 8UL, null, null);
        _grain.GetInformation().Returns(new ObserverInformationSnapshot(definition, new ObserverState(), true, null) { LagSample = sample });
    }

    async Task Because() => _result = await Query();

    [Fact] void should_use_the_default_sequence() => _result.EventSequenceId.ShouldEqual(EventSequenceId.Log.Value);
    [Fact] void should_return_the_resident_sample() => _result.Status.ShouldEqual(ObserverLagStatus.Lagging);
}
