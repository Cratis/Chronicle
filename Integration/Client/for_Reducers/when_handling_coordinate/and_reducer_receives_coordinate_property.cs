// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reducers;
using Cratis.Geospatial;
using context = Cratis.Chronicle.Integration.for_Reducers.when_handling_coordinate.and_reducer_receives_coordinate_property.context;

namespace Cratis.Chronicle.Integration.for_Reducers.when_handling_coordinate;

[Collection(ChronicleCollection.Name)]
public class and_reducer_receives_coordinate_property(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification(chronicleFixture)
    {
        public CoordinateReducer Reducer { get; private set; } = default!;
        public CoordinateEvent Event { get; private set; } = default!;

        public override IEnumerable<Type> EventTypes => [typeof(CoordinateEvent)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            Reducer = new CoordinateReducer();
            services.AddSingleton(Reducer);
        }

        async Task Because()
        {
            await EventStore.ReadModels.Register<CoordinateReadModel>();
            var reducer = await EventStore.Reducers.Register<CoordinateReducer, CoordinateReadModel>();
            await reducer.WaitTillSubscribed();

            Event = new CoordinateEvent(new Coordinate(51.5074, -0.1278));
            await EventStore.EventLog.Append("location-1", Event);
            await Reducer.WaitTillHandledEventReaches(1);
        }
    }

    [Fact] void should_receive_correct_longitude() => Context.Reducer.LastLocation.Longitude.ShouldEqual(Context.Event.Location.Longitude);
    [Fact] void should_receive_correct_latitude() => Context.Reducer.LastLocation.Latitude.ShouldEqual(Context.Event.Location.Latitude);
}
