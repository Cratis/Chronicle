// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.given;

public class a_replayable_observer : all_dependencies
{
    protected static readonly ObserverKey _key = new("some-observer", "some-event-store", "some-namespace", "inbox-source");
    protected Chronicle.Observation.IObserver _observer;

    void Establish()
    {
        _observer = Substitute.For<Chronicle.Observation.IObserver>();
        _grainFactory.GetGrain<Chronicle.Observation.IObserver>(Arg.Any<string>(), Arg.Any<string>()).Returns(_observer);
        _observerDefinitionsStorage.Get(Arg.Any<ObserverId>()).Returns(new ObserverDefinition(
            _key.ObserverId, [], _key.EventSequenceId, ObserverType.Reactor, ObserverOwner.Client, true));
    }
}
