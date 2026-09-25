// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_delivering_to_a_delegate;

public class with_pending_work : given.a_registered_delegate
{
    ReactorEvent _event;
    bool _acknowledgedBeforeCompletion;
    ReactorResult _acknowledgement;

    async Task Because()
    {
        var delivery = Task.Run(Deliver);
        _event = await _received.Task.WaitAsync(TimeSpan.FromSeconds(10));
        _acknowledgedBeforeCompletion = _result.Task.IsCompleted;
        _release.SetResult();
        _acknowledgement = await _result.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await delivery.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_not_acknowledge_before_the_delegate_completes() => _acknowledgedBeforeCompletion.ShouldBeFalse();
    [Fact] void should_acknowledge_after_completion() => _acknowledgement.State.ShouldEqual(ObservationState.Success);
    [Fact] void should_keep_the_delivered_generation() => _event.Context.EventType.Generation.Value.ShouldEqual(1u);
    [Fact] void should_pass_the_json_content() => _event.Content["order"]!.GetValue<int>().ShouldEqual(42);
    [Fact] void should_pass_the_unmodified_generational_content() => _event.GenerationalContent[2].ShouldEqual("{\"orderId\":42}");
    [Fact] void should_register_both_generations() => _definition.EventTypes.Count.ShouldEqual(2);
    [Fact] void should_keep_the_replay_policy() => _definition.IsReplayable.ShouldBeFalse();
    [Fact] void should_default_to_the_event_log() => _definition.EventSequenceId.ShouldEqual(EventSequenceId.LogId);
    [Fact] void should_not_filter_out_other_event_source_types() => _definition.Filters.EventSourceType.ShouldEqual(EventSourceType.Unspecified.Value);
    [Fact] void should_not_require_clr_event_deserialization() => _eventSerializer.ReceivedCalls().Any(call => call.GetMethodInfo().Name == nameof(_eventSerializer.Deserialize)).ShouldBeFalse();
}
