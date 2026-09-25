// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using context = Cratis.Chronicle.MongoDB.Integration.Observation.for_ObserverStateStorage.when_deleting_the_state.context;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.MongoDB.Integration.Observation.for_ObserverStateStorage;

/// <summary>
/// The document has to actually leave the collection. Saving writes a targeted update with an upsert, so a delete
/// implemented as "save an empty state" would leave a document behind that still lists the observer - which is
/// precisely the orphaned record removing an observer is meant to get rid of.
/// </summary>
/// <param name="context">The <see cref="context"/> the specification runs against.</param>
[Collection(MongoDBCollection.Name)]
public class when_deleting_the_state(context context) : MongoDBGiven<context>(context)
{
    public class context(ChronicleInProcessFixture fixture) : given.an_observer_state_storage(fixture)
    {
        public static readonly ObserverId TheObserver = "the-observer";
        public static readonly ObserverId AnotherObserver = "another-observer";

        public KernelObserverState StateAfterDelete = default!;
        public IEnumerable<KernelObserverState> AllAfterDelete = default!;

        async Task Because()
        {
            await _storage.Save(CreateState(TheObserver));
            await _storage.Save(CreateState(AnotherObserver));

            await _storage.Delete(TheObserver);

            StateAfterDelete = await _storage.Get(TheObserver);
            AllAfterDelete = await _storage.GetAll();
        }
    }

    [Fact] void should_no_longer_have_a_state_for_the_observer() => Context.StateAfterDelete.Identifier.ShouldEqual(ObserverId.Unspecified);
    [Fact] void should_leave_exactly_one_observer_behind() => Context.AllAfterDelete.Count().ShouldEqual(1);
    [Fact] void should_leave_the_other_observer_untouched() => Context.AllAfterDelete.Single().Identifier.ShouldEqual(context.AnotherObserver);
}
