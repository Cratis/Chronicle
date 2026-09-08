// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverPartitionCommands.given;

public class an_observer_grain : Specification
{
    protected const string EventStore = "some-event-store";
    protected const string Namespace = "some-namespace";
    protected const string ObserverIdentifier = "some-observer";
    protected const string Partition = "some-partition";

    protected IGrainFactory _grainFactory;
    protected IObserver _observer;

    void Establish()
    {
        _observer = Substitute.For<IObserver>();
        _grainFactory = Substitute.For<IGrainFactory>();
        _grainFactory.GetGrain<IObserver>(Arg.Any<string>(), Arg.Any<string>()).Returns(_observer);
    }
}
