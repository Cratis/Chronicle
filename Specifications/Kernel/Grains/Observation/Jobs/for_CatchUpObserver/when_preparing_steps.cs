// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Kernel.Grains.Jobs;
using Aksio.Cratis.Kernel.Keys;
using Aksio.Cratis.Kernel.Storage.Keys;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Kernel.Grains.Observation.Jobs.for_CatchUpObserver;

public class when_preparing_steps : given.a_catchup_observer_and_a_request
{
    Mock<IObserverKeyIndex> index;
    IEnumerable<Key> keys;
    IImmutableList<JobStepDetails> job_steps;

    void Establish()
    {
        index = new();
        observer_key_indexes.Setup(_ => _.GetFor(((CatchUpObserverRequest)state_storage.State.Request).ObserverId, ((CatchUpObserverRequest)state_storage.State.Request).ObserverKey)).ReturnsAsync(index.Object);

        keys = new[]
        {
            new Key(Guid.NewGuid(), ArrayIndexers.NoIndexers),
            new Key(Guid.NewGuid(), ArrayIndexers.NoIndexers),
            new Key(Guid.NewGuid(), ArrayIndexers.NoIndexers)
        };
        index.Setup(_ => _.GetKeys(((CatchUpObserverRequest)state_storage.State.Request).FromEventSequenceNumber)).Returns(() => new ObserverKeysForTesting(keys));
    }

    async Task Because() => job_steps = await job.WrappedPrepareSteps((CatchUpObserverRequest)state_storage.State.Request);

    [Fact] void should_generate_same_amount_of_steps_as_keys() => job_steps.Count.ShouldEqual(keys.Count());
    [Fact] void should_set_correct_observer_id_for_all_steps() => job_steps.All(_ => ((HandleEventsForPartitionArguments)_.Request).ObserverId == ((CatchUpObserverRequest)state.Request).ObserverId).ShouldBeTrue();
    [Fact] void should_set_correct_observer_key_for_all_steps() => job_steps.All(_ => ((HandleEventsForPartitionArguments)_.Request).ObserverKey == ((CatchUpObserverRequest)state.Request).ObserverKey).ShouldBeTrue();
    [Fact] void should_set_correct_subscription_for_all_steps() => job_steps.All(_ => ((HandleEventsForPartitionArguments)_.Request).ObserverSubscription == ((CatchUpObserverRequest)state.Request).ObserverSubscription).ShouldBeTrue();
    [Fact] void should_correct_keys() => job_steps.Select(_ => ((HandleEventsForPartitionArguments)_.Request).Partition).ShouldContainOnly(keys);
    [Fact] void should_set_correct_from_event_sequence_number() => job_steps.All(_ => ((HandleEventsForPartitionArguments)_.Request).StartEventSequenceNumber == ((CatchUpObserverRequest)state.Request).FromEventSequenceNumber).ShouldBeTrue();
    [Fact] void should_set_correct_event_types() => job_steps.All(_ => ((HandleEventsForPartitionArguments)_.Request).EventTypes == ((CatchUpObserverRequest)state.Request).EventTypes).ShouldBeTrue();
}
