// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.given;

public class a_scenario_with_closing_events : Specification, IDisposable
{
    protected EventScenario _scenario;
    protected EventTypeId _bookedType;

    void Establish()
    {
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.EventTypes.Returns([typeof(BooksClosed), typeof(BooksReopened), typeof(Booked)]);
        artifacts.ClosesStreamEventTypes.Returns([typeof(BooksClosed)]);
        var defaults = new Defaults(artifacts);
        _bookedType = defaults.EventTypes.GetEventTypeFor(typeof(Booked)).Id;
        _scenario = new(defaults);
    }

    public void Dispose() => _scenario.Dispose();

    [EventType]
    [ClosesStream(Name = "close-books", Dimensions = ClosedStreamDimensions.EventSourceId | ClosedStreamDimensions.EventStreamId, EventStreamIdFrom = nameof(Period), ReopenedBy = [typeof(BooksReopened)])]
    protected record BooksClosed(string Period);

    [EventType]
    protected record BooksReopened(string Period);

    [EventType]
    protected record Booked(string Description);
}
