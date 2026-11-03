// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.for_ProjectionPartitionRebuilder;

public class when_a_timed_out_delivery_is_retried : given.a_rebuilder
{
    readonly TaskCompletionSource _applied = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _acknowledge = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _sum;
    ExpandoObject? _published;

    async Task Establish()
    {
        _pipeline.Handle(Arg.Any<AppendedEvent>()).Returns(async call =>
        {
            var @event = (AppendedEvent)call[0];
            _sum += 7;
            var state = new ExpandoObject();
            ((IDictionary<string, object?>)state)["sum"] = _sum;
            var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), @event, new ExpandoObject());
            new ObjectComparer().Compare(changeset.InitialState, state, out var differences);
            changeset.ReplaceState(state, differences);
            await _isolatedSink.ApplyChanges(_partition, changeset, @event.Context.SequenceNumber);
            _applied.TrySetResult();
            await _acknowledge.Task;
            return new ProjectionEventContext(_partition, @event, changeset, ProjectionOperationType.From, false);
        });
        _liveSink.ReplaceIsolated(_partition, Arg.Any<ExpandoObject?>(), (EventSequenceNumber)1UL).Returns(call =>
        {
            _published = (ExpandoObject?)call[1];
            return IsolatedReplaceResult.Replaced;
        });
        await _rebuilder.Begin(_observerKey, _partition);
    }

    async Task Because()
    {
        var abandonedDelivery = _rebuilder.OnNext(_partition, [EventAt(1)], new(null));
        await _applied.Task;
        // A caller timeout does not cancel the already applied subscriber call. Redeliver before it acknowledges.
        var retry = _rebuilder.OnNext(_partition, [EventAt(1)], new(null));
        _acknowledge.SetResult();
        await abandonedDelivery;
        await retry;
        await _rebuilder.Publish(1UL);
    }

    [Fact] void should_apply_the_increment_once() => _sum.ShouldEqual(7);
    [Fact] void should_publish_the_increment_once() => ((IDictionary<string, object?>)_published!)["sum"].ShouldEqual(7);
    [Fact] void should_invoke_the_pipeline_once() => _pipeline.Received(1).Handle(Arg.Any<AppendedEvent>());
}
