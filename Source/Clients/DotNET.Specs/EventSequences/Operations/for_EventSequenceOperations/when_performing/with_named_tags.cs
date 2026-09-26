// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Operations.for_EventSequenceOperations.when_performing;

public class with_named_tags : given.event_sequence_operations_without_any_operations
{
    EventForEventSourceId[] _submitted;
    int _enumerations;
    IEnumerable<NamedTag> _previewTags;

    void Establish()
    {
        _operations.ForEventSourceId(EventSourceId.New(), builder =>
        {
            var staged = (List<IEventSequenceOperation>)typeof(EventSourceOperations)
                .GetField("_operations", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(builder)!;
            staged.Add(new AppendOperation("event") { NamedTags = YieldOnce() });
        });
        _previewTags = _operations.GetEventsToAppend().Single().NamedTags;
        _eventSequence.AppendMany(
            Arg.Any<IEnumerable<EventForEventSourceId>>(),
            Arg.Any<IEnumerable<NamedTag>>(),
            Arg.Any<CorrelationId?>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<IDictionary<EventSourceId, EventSequences.Concurrency.ConcurrencyScope>>())
            .Returns(call =>
            {
                _submitted = call.Arg<IEnumerable<EventForEventSourceId>>().ToArray();
                return new AppendManyResult();
            });
    }

    async Task Because() => await _operations.Perform();

    [Fact] void should_keep_tags_after_previewing_events() => _previewTags.Single().Value.ShouldEqual("value");
    [Fact] void should_submit_the_named_tags_once() => _submitted.Single().NamedTags.Single().Value.ShouldEqual("value");
    [Fact] void should_enumerate_input_once() => _enumerations.ShouldEqual(1);
    [Fact] void should_not_use_legacy_append() => _eventSequence.DidNotReceive().AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>>(), Arg.Any<IDictionary<EventSourceId, EventSequences.Concurrency.ConcurrencyScope>>());

    IEnumerable<NamedTag> YieldOnce()
    {
        _enumerations++;
        if (_enumerations != 1)
        {
            throw new Exception("Named tags were enumerated more than once");
        }
        yield return new NamedTag("key", "value");
    }
}
