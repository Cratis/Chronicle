// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Integration.for_ReadModels;
using Cratis.Chronicle.Reducers;

using context = Cratis.Chronicle.Integration.for_Reducers.when_appending_across_many_event_sources.and_the_events_arrive_in_one_burst.context;

namespace Cratis.Chronicle.Integration.for_Reducers.when_appending_across_many_event_sources;

/// <summary>
/// One append spanning many event sources reaches the reducer in pages spanning those event sources, and every read model
/// folds exactly the events of its own event source, as it would one partition at a time.
/// </summary>
/// <param name="context">The <see cref="context"/> the specification runs against.</param>
[Collection(ChronicleCollection.Name)]
public class and_the_events_arrive_in_one_burst(context context) : Given<context>(context)
{
    const int NumberOfEventSources = 50;
    const int EventsPerEventSource = 3;

    public class context(ChronicleFixture chronicleFixture) : Specification(chronicleFixture)
    {
        public List<NullableCounter> Persisted = [];

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
        public override IEnumerable<Type> Reducers => [typeof(NullableCounterReducer)];

        protected override void ConfigureServices(IServiceCollection services) => services.AddSingleton<NullableCounterReducer>();

        async Task Because()
        {
            var reducer = EventStore.Reducers.GetHandlerFor<NullableCounterReducer>();
            await reducer.WaitTillSubscribed();

            // Interleaved event sources in one append, as a burst of writes from many event sources produces.
            var events = Enumerable.Range(0, EventsPerEventSource)
                .SelectMany(round => Enumerable.Range(0, NumberOfEventSources).Select(source => new EventForEventSourceId($"counter-{source}", new SomeEvent(round + 1))))
                .ToArray();
            var result = await EventStore.EventLog.AppendMany(events);
            await reducer.WaitTillReachesEventSequenceNumber(result.TailSequenceNumber, TimeSpan.FromSeconds(60));

            for (var source = 0; source < NumberOfEventSources; source++)
            {
                Persisted.Add(await StoredReadModelDocument.ReadInstance<NullableCounter>(EventStore, $"counter-{source}"));
            }
        }
    }

    [Fact] void should_have_a_read_model_for_every_event_source() => Context.Persisted.Count(_ => _ is not null).ShouldEqual(NumberOfEventSources);
    [Fact] void should_fold_every_event_of_each_event_source_once() => Context.Persisted.TrueForAll(_ => _.Count == 1 + 2 + 3).ShouldBeTrue();
}
