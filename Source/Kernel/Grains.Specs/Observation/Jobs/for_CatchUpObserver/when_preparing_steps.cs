// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Keys;

namespace Cratis.Chronicle.Grains.Observation.Jobs.for_CatchUpObserver;

public class when_preparing_steps : given.a_catchup_observer_and_a_request
{
    IObserverKeyIndex _index;
    IEnumerable<Key> _keys;
    IImmutableList<JobStepDetails> _jobSteps;

    void Establish()
    {
        _index = Substitute.For<IObserverKeyIndex>();
        _observerKeyIndexes.GetFor(((CatchUpObserverRequest)_stateStorage.State.Request).ObserverKey).Returns(_index);

        _keys =
        [
            new Key(Guid.NewGuid(), ArrayIndexers.NoIndexers),
            new Key(Guid.NewGuid(), ArrayIndexers.NoIndexers),
            new Key(Guid.NewGuid(), ArrayIndexers.NoIndexers)
        ];
        _index.GetKeys(((CatchUpObserverRequest)_stateStorage.State.Request).FromEventSequenceNumber).Returns(_ => new ObserverKeysForTesting(_keys));
    }

    async Task Because() => _jobSteps = await _job.WrappedPrepareSteps((CatchUpObserverRequest)_stateStorage.State.Request);

    [Fact] void should_generate_same_amount_of_steps_as_keys() => _jobSteps.Count.ShouldEqual(_keys.Count());
    [Fact] void should_set_correct_observer_key_for_all_steps() => _jobSteps.All(_ => ((HandleEventsForPartitionArguments)_.Request).ObserverKey == ((CatchUpObserverRequest)_state.Request).ObserverKey).ShouldBeTrue();
    [Fact] void should_set_correct_subscription_for_all_steps() => _jobSteps.All(_ => ((HandleEventsForPartitionArguments)_.Request).ObserverSubscription == ((CatchUpObserverRequest)_state.Request).ObserverSubscription).ShouldBeTrue();
    [Fact] void should_correct_keys() => _jobSteps.Select(_ => ((HandleEventsForPartitionArguments)_.Request).Partition).ShouldContainOnly(_keys);
    [Fact] void should_set_correct_from_event_sequence_number() => _jobSteps.All(_ => ((HandleEventsForPartitionArguments)_.Request).StartEventSequenceNumber == ((CatchUpObserverRequest)_state.Request).FromEventSequenceNumber).ShouldBeTrue();
    [Fact] void should_set_correct_event_types() => _jobSteps.All(_ => ((HandleEventsForPartitionArguments)_.Request).EventTypes == ((CatchUpObserverRequest)_state.Request).EventTypes).ShouldBeTrue();
}
