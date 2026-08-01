// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Api.Observation.for_ObserverCommands.given;

public class observer_commands : Specification
{
    protected const string EventStore = "some-event-store";
    protected const string Namespace = "some-namespace";
    protected const string ObserverId = "some-observer";
    protected const string Partition = "some-partition";

    protected ObserverCommands _commands;
    protected IObservers _observers;

    void Establish()
    {
        _observers = Substitute.For<IObservers>();
        _commands = new ObserverCommands(_observers);
    }
}
