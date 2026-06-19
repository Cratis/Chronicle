// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reducers;
using context = Cratis.Chronicle.Integration.for_Reducers.when_handling_concept_collection.and_reducer_persists_the_concept_collection.context;

namespace Cratis.Chronicle.Integration.for_Reducers.when_handling_concept_collection;

[Collection(ChronicleCollection.Name)]
public class and_reducer_persists_the_concept_collection(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification(chronicleFixture)
    {
        const string EventSourceId = "surface-1";

        public ConceptCollectionReducer Reducer { get; private set; } = default!;
        public ConceptCollectionReadModel Persisted { get; private set; } = default!;

        public override IEnumerable<Type> EventTypes => [typeof(ConceptCollectionItemAdded)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            Reducer = new ConceptCollectionReducer();
            services.AddSingleton(Reducer);
        }

        async Task Because()
        {
            await EventStore.ReadModels.Register<ConceptCollectionReadModel>();
            var reducer = await EventStore.Reducers.Register<ConceptCollectionReducer, ConceptCollectionReadModel>();
            await reducer.WaitTillSubscribed();

            await EventStore.EventLog.Append(EventSourceId, new ConceptCollectionItemAdded(new ConceptCollectionItem("React")));
            await EventStore.EventLog.Append(EventSourceId, new ConceptCollectionItemAdded(new ConceptCollectionItem("TypeScript")));
            await Reducer.WaitTillHandledEventReaches(2);

            // Read the materialized instance back from the sink (storage-agnostic) so the assertion
            // reflects what was actually persisted and deserialized — not the in-memory reducer state.
            Persisted = await WaitForPersistedInstance(TimeSpan.FromSeconds(10));
        }

        async Task<ConceptCollectionReadModel> WaitForPersistedInstance(TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            while (!cts.IsCancellationRequested)
            {
                var instances = await EventStore.ReadModels.Materialized.GetInstances<ConceptCollectionReadModel>();
                var instance = instances.FirstOrDefault(_ => _.Latest is not null);
                if (instance is { Items.Count: >= 2 })
                {
                    return instance;
                }

                await Task.Delay(100, CancellationToken.None);
            }

            return null!;
        }
    }

    [Fact] void should_persist_the_instance() => Context.Persisted.ShouldNotBeNull();
    [Fact] void should_persist_the_scalar_concept() => Context.Persisted.Latest.Value.ShouldEqual("TypeScript");
    [Fact] void should_persist_the_whole_concept_collection() => Context.Persisted.Items.Count.ShouldEqual(2);
    [Fact] void should_persist_the_first_concept_value() => Context.Persisted.Items.Select(_ => _.Value).ShouldContain("React");
    [Fact] void should_persist_the_second_concept_value() => Context.Persisted.Items.Select(_ => _.Value).ShouldContain("TypeScript");
}
