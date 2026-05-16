// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reducers;
using context = Cratis.Chronicle.Integration.for_Reducers.when_handling_pii_events.and_reducer_receives_decrypted_pii_property.context;

namespace Cratis.Chronicle.Integration.for_Reducers.when_handling_pii_events;

[Collection(ChronicleCollection.Name)]
public class and_reducer_receives_decrypted_pii_property(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification(chronicleFixture)
    {
        public ReducerWithoutDelayHandlingPii Reducer { get; private set; } = default!;
        public PiiEvent Event { get; private set; } = default!;

        public override IEnumerable<Type> EventTypes => [typeof(PiiEvent)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            Reducer = new ReducerWithoutDelayHandlingPii();
            services.AddSingleton(Reducer);
        }

        async Task Because()
        {
            await EventStore.ReadModels.Register<SomeReadModel>();
            var reducer = await EventStore.Reducers.Register<ReducerWithoutDelayHandlingPii, SomeReadModel>();
            await reducer.WaitTillSubscribed();

            Event = new PiiEvent(42, "111-22-3333");
            await EventStore.EventLog.Append("person-1", Event);
            await Reducer.WaitTillHandledEventReaches(1);
        }
    }

    [Fact] void should_receive_decrypted_pii_property() => Context.Reducer.LastSocialSecurityNumber.ShouldEqual(Context.Event.SocialSecurityNumber);
}
