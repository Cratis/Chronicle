// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.for_ReadModels;
using Cratis.Chronicle.Reducers;

using context = Cratis.Chronicle.Integration.for_Reducers.when_reducing_the_same_key_in_separate_batches.context;

namespace Cratis.Chronicle.Integration.for_Reducers;

[Collection(ChronicleCollection.Name)]
public class when_reducing_the_same_key_in_separate_batches(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification(chronicleFixture)
    {
        public NullableCounter Persisted;

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
        public override IEnumerable<Type> Reducers => [typeof(NullableCounterReducer)];

        protected override void ConfigureServices(IServiceCollection services) => services.AddSingleton<NullableCounterReducer>();

        async Task Because()
        {
            var reducer = EventStore.Reducers.GetHandlerFor<NullableCounterReducer>();
            await reducer.WaitTillSubscribed();
            var first = await EventStore.EventLog.Append("counter-1", new SomeEvent(1));
            await reducer.WaitTillReachesEventSequenceNumber(first.SequenceNumber);
            var second = await EventStore.EventLog.Append("counter-1", new SomeEvent(2));
            await reducer.WaitTillReachesEventSequenceNumber(second.SequenceNumber);
            Persisted = await StoredReadModelDocument.ReadInstance<NullableCounter>(EventStore, "counter-1");
        }
    }

    [Fact] void should_accumulate_both_batches() => Context.Persisted.Count.ShouldEqual(3);
    [Fact] void should_preserve_the_null_property() => Context.Persisted.Note.ShouldBeNull();
}
