// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Projections.Events;
using Cratis.Chronicle.Integration.Projections.ProjectionTypes;
using Cratis.Chronicle.Integration.Projections.ReadModels;
using Cratis.Geospatial;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_coordinate.and_coordinate_value_round_trips.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_coordinate;

[Collection(ChronicleCollection.Name)]
public class and_coordinate_value_round_trips(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : given.a_projection_and_events_appended_to_it<CoordinateProjection, CoordinateReadModel>(chronicleFixture)
    {
        public CoordinateEvent EventAppended;

        public override IEnumerable<Type> EventTypes => [typeof(CoordinateEvent)];

        void Establish()
        {
            EventAppended = new CoordinateEvent(new Coordinate(51.5074, -0.1278));
            EventsToAppend.Add(EventAppended);
        }
    }

    [Fact] void should_set_the_location() => Context.Result.Location.ShouldEqual(Context.EventAppended.Location);
    [Fact] void should_preserve_longitude() => Context.Result.Location.Longitude.ShouldEqual(Context.EventAppended.Location.Longitude);
    [Fact] void should_preserve_latitude() => Context.Result.Location.Latitude.ShouldEqual(Context.EventAppended.Location.Latitude);
}
