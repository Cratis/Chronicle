// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.Copy.for_CopyEventSourceEventsStep;

public class when_performing_without_event_types : given.the_job_step
{
    async Task Because() => await Perform();

    [Fact] void should_succeed() => _result.TryGetException(out _).ShouldBeFalse();
    [Fact] void should_read_from_the_source() => _requestedSource.ShouldEqual(_sourceId);
    [Fact] void should_not_narrow_by_event_type() => _requestedEventTypes.ShouldBeNull();
    [Fact] void should_append_every_event() => _appended.Count.ShouldEqual(2);
    [Fact] void should_append_to_the_target() => _appended.All(_ => _.EventSourceId == _targetId).ShouldBeTrue();
    [Fact] void should_keep_the_event_type() => _appended.All(_ => _.EventType.Id == _eventTypeId).ShouldBeTrue();
    [Fact] void should_keep_the_tags() => _appended.All(_ => _.Tags.Any(tag => tag.Value == "a-tag")).ShouldBeTrue();
    [Fact] void should_keep_the_content() => _appended.Select(_ => _.Content["name"]!.GetValue<string>()).ShouldContainOnly("first", "second");
    [Fact] void should_make_the_target_the_subject() => _appended.All(_ => _.Subject!.Value == "target").ShouldBeTrue();
    [Fact] void should_key_each_copy_to_the_job_and_source_event() =>
        _keys.Select(_ => _!.Value).ShouldContainOnly($"copy:{_jobStepKey.JobId}:1", $"copy:{_jobStepKey.JobId}:2");
}
