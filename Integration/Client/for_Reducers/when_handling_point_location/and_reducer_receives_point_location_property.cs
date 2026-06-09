// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reducers;
using Cratis.Geospatial;
using context = Cratis.Chronicle.Integration.for_Reducers.when_handling_point_location.and_reducer_receives_point_location_property.context;

namespace Cratis.Chronicle.Integration.for_Reducers.when_handling_point_location;

[Collection(ChronicleCollection.Name)]
public class and_reducer_receives_point_location_property(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification(chronicleFixture)
    {
        public PointLocationReducer Reducer { get; private set; } = default!;
        public PointLocationEvent Event { get; private set; } = default!;

        public override IEnumerable<Type> EventTypes => [typeof(PointLocationEvent)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            Reducer = new PointLocationReducer();
            services.AddSingleton(Reducer);
        }

        async Task Because()
        {
            await EventStore.ReadModels.Register<PointLocationReadModel>();
            var reducer = await EventStore.Reducers.Register<PointLocationReducer, PointLocationReadModel>();
            await reducer.WaitTillSubscribed();

            Event = new PointLocationEvent(new Point(51.5074, -0.1278));
            await EventStore.EventLog.Append("location-1", Event);
            await Reducer.WaitTillHandledEventReaches(1);
        }
    }

    [Fact] void should_receive_correct_longitude() => Context.Reducer.LastLocation.Longitude.ShouldEqual(Context.Event.Location.Longitude);
    [Fact] void should_receive_correct_latitude() => Context.Reducer.LastLocation.Latitude.ShouldEqual(Context.Event.Location.Latitude);
}
