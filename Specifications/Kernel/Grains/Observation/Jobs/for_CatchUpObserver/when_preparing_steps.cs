// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Kernel.Grains.Jobs;
using Aksio.Cratis.Kernel.Keys;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Kernel.Grains.Observation.Jobs.for_CatchUpObserver;

public class when_preparing_steps : given.a_catchup_observer
{
    Mock<IObserverKeyIndex> index;
    CatchUpObserverRequest request;
    IEnumerable<Key> keys;
    IImmutableList<JobStepDetails> job_steps;

    void Establish()
    {
        request = new(
            Guid.NewGuid(),
            new ObserverKey(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            ObserverSubscription.Unsubscribed,
            42UL,
            new[] {
                new EventType(Guid.NewGuid(), EventGeneration.First),
                new EventType(Guid.NewGuid(), EventGeneration.First)
            });

        index = new();
        indexes.Setup(_ => _.GetFor(request.ObserverId, request.ObserverKey)).ReturnsAsync(index.Object);

        keys = new[]
        {
            new Key(Guid.NewGuid(), ArrayIndexers.NoIndexers),
            new Key(Guid.NewGuid(), ArrayIndexers.NoIndexers),
            new Key(Guid.NewGuid(), ArrayIndexers.NoIndexers)
        };
        index.Setup(_ => _.GetKeys(request.FromEventSequenceNumber)).Returns(() => new ObserverKeysForTesting(keys));
    }

    async Task Because() => job_steps = await job.WrappedPrepareSteps(request);

    [Fact] void should_generate_same_amount_of_steps_as_keys() => job_steps.Count.ShouldEqual(keys.Count());
    [Fact] void should_set_correct_observer_id_for_all_steps() => job_steps.All(_ => ((HandleEventsForPartitionArguments)_.Request).ObserverId == request.ObserverId).ShouldBeTrue();
    [Fact] void should_set_correct_observer_key_for_all_steps() => job_steps.All(_ => ((HandleEventsForPartitionArguments)_.Request).ObserverKey == request.ObserverKey).ShouldBeTrue();
    [Fact] void should_set_correct_subscription_for_all_steps() => job_steps.All(_ => ((HandleEventsForPartitionArguments)_.Request).ObserverSubscription == request.ObserverSubscription).ShouldBeTrue();
    [Fact] void should_correct_keys() => job_steps.Select(_ => ((HandleEventsForPartitionArguments)_.Request).Partition).ShouldContainOnly(keys);
    [Fact] void should_set_correct_from_event_sequence_number() => job_steps.All(_ => ((HandleEventsForPartitionArguments)_.Request).StartEventSequenceNumber == request.FromEventSequenceNumber).ShouldBeTrue();
    [Fact] void should_set_correct_event_types() => job_steps.All(_ => ((HandleEventsForPartitionArguments)_.Request).EventTypes == request.EventTypes).ShouldBeTrue();
}
