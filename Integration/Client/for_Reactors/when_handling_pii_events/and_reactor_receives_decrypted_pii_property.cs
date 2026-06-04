// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.for_Reactors.when_handling_pii_events.and_reactor_receives_decrypted_pii_property.context;

namespace Cratis.Chronicle.Integration.for_Reactors.when_handling_pii_events;

[Collection(ChronicleCollection.Name)]
public class and_reactor_receives_decrypted_pii_property(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification(chronicleFixture)
    {
        public ReactorWithoutDelayHandlingPii Reactor { get; private set; } = default!;
        public PiiEvent Event { get; private set; } = default!;

        public override IEnumerable<Type> EventTypes => [typeof(PiiEvent)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            Reactor = new ReactorWithoutDelayHandlingPii();
            services.AddSingleton(Reactor);
        }

        async Task Because()
        {
            var reactor = await EventStore.Reactors.Register<ReactorWithoutDelayHandlingPii>();
            await reactor.WaitTillSubscribed();

            Event = new PiiEvent(42, "111-22-3333");
            await EventStore.EventLog.Append("person-1", Event);
            await Reactor.WaitTillHandledEventReaches(1);
        }
    }

    [Fact] void should_receive_decrypted_pii_property() => Context.Reactor.LastSocialSecurityNumber.ShouldEqual(Context.Event.SocialSecurityNumber);
}
