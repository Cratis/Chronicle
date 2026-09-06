// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Patterns;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Patterns.for_PatternCaptureSubscriber.when_observing_events;

/// <summary>
/// The subscriber resolves the miner by its own key's event store and namespace - that resolution is the
/// isolation: the same scope name in two stores or two tenants' namespaces reaches two different miner grains and
/// can never count into one sketch.
/// </summary>
public class and_events_name_scopes : given.a_pattern_capture_subscriber
{
    static readonly PatternGroupingKey _scope = "some.user";

    ObserverSubscriberResult _result;

    async Task Because() =>
        _result = await _subscriber.OnNext(
            new Key("partition", ArrayIndexers.NoIndexers),
            [EventAt(42UL, _scope), EventAt(43UL, _scope)],
            new ObserverSubscriberContext(null));

    [Fact] void should_answer_ok() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_answer_with_the_tail_of_the_batch() => _result.LastSuccessfulObservation.ShouldEqual((EventSequenceNumber)43UL);
    [Fact] async Task should_hand_the_batch_to_the_miner_in_one_call() =>
        await _miner.Received(1).Mine(Arg.Is<IEnumerable<EventFeatures>>(features => features.Count() == 2 && features.All(feature => feature.GroupingKey == _scope)));
    [Fact] void should_reach_the_miner_of_its_own_event_store_and_namespace() =>
        _minerIdentity.ShouldEqual(new PatternMinerKey(_eventStore, _namespace).ToString());
}
