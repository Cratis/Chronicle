// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering_a_delegate;

public class and_using_default_options : given.a_registered_delegate
{
    [Fact] void should_register_both_generations() => _definition.EventTypes.Count.ShouldEqual(2);
    [Fact] void should_keep_the_replay_policy() => _definition.IsReplayable.ShouldBeFalse();
    [Fact] void should_default_to_the_event_log() => _definition.EventSequenceId.ShouldEqual(EventSequenceId.LogId);
    [Fact] void should_not_filter_out_other_event_source_types() => _definition.Filters.EventSourceType.ShouldEqual(EventSourceType.Unspecified.Value);
    [Fact] void should_report_object_as_the_reactor_type() => _handler.ReactorType.ShouldEqual(typeof(object));
}
