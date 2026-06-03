// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_getting_instances.and_read_model_has_pii_property.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_getting_instances;

[Collection(ChronicleCollection.Name)]
public class and_read_model_has_pii_property(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification(chronicleFixture)
    {
        public EventSourceId EventSourceId { get; } = "person-1";
        public PiiEvent Event { get; private set; } = default!;
        public IEnumerable<PiiReadModel> Instances { get; private set; } = default!;

        public override IEnumerable<Type> EventTypes => [typeof(PiiEvent)];
        public override IEnumerable<Type> Projections => [typeof(PiiProjection)];

        void Establish() => Event = new PiiEvent("Alice", "100-11-2233");

        async Task Because()
        {
            await EventStore.EventLog.Append(EventSourceId, Event);
            Instances = await EventStore.ReadModels.GetInstances<PiiReadModel>();
        }
    }

    [Fact] void should_return_single_instance() => Context.Instances.Count().ShouldEqual(1);
    [Fact] void should_return_decrypted_pii_property() => Context.Instances.First().SocialSecurityNumber.ShouldEqual(Context.Event.SocialSecurityNumber);
}
