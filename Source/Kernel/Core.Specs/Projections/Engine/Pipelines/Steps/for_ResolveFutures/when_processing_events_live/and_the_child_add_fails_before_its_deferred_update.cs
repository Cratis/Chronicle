// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveFutures.when_processing_events_live;

public class and_the_child_add_fails_before_its_deferred_update : given.deferred_child_events
{
    int _futureWrites;
    int _remainingAfterFailure;
    int _remainingAfterRetry;
    IDictionary<string, object?>[] _childrenAfterFailure;
    IDictionary<string, object?>[] _childrenAfterRetry;
    ProjectionEventContext _result;

    async Task Because()
    {
        await Process(Event(_childAdded, 1, new { id = "child", name = "Child", email = "initial@example.com" }));
        await Process(Event(_emailChanged, 2, new { id = "child", email = "updated@example.com" }));
        var context = await Process(Event(_rootCreated, 3, new { name = "Root" }), save: false);
        var failingSink = Substitute.For<ISink>();
        failingSink.ApplyChanges(Arg.Any<Key>(), Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>(), Arg.Any<EventSequenceNumber>(), Arg.Any<SinkWriteMode>())
            .Returns(call => _sink.ApplyChanges(call.Arg<Key>(), call.Arg<IChangeset<AppendedEvent, ExpandoObject>>(), call.Arg<EventSequenceNumber>(), call.Arg<SinkWriteMode>()));
        failingSink.ApplyChangesImmediately(Arg.Any<Key>(), Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>(), Arg.Any<EventSequenceNumber>(), Arg.Any<SinkWriteMode>())
            .Returns(call =>
            {
                _futureWrites++;
                return _futureWrites == 1
                    ? Task.FromResult<IEnumerable<FailedPartition>>([new FailedPartition(context.Key, context.EventSequenceNumber)])
                    : _sink.ApplyChanges(call.Arg<Key>(), call.Arg<IChangeset<AppendedEvent, ExpandoObject>>(), call.Arg<EventSequenceNumber>(), call.Arg<SinkWriteMode>());
            });
        var save = new SaveChanges(_futures, failingSink, Substitute.For<IChangesetStorage>(), false, NullLogger<SaveChanges>.Instance);
        _result = await save.Perform(_projection, context);
        _remainingAfterFailure = (await _futures.GetFutures()).Count();
        _childrenAfterFailure = await StoredChildren(context.Key);

        await _save.Perform(_projection, context);
        _remainingAfterRetry = (await _futures.GetFutures()).Count();
        _childrenAfterRetry = await StoredChildren(context.Key);
    }

    [Fact] void should_report_the_failed_add() => _result.FailedPartitions.Count().ShouldEqual(1);
    [Fact] void should_not_attempt_the_dependent_update() => _futureWrites.ShouldEqual(1);
    [Fact] void should_keep_both_futures_for_retry() => _remainingAfterFailure.ShouldEqual(2);
    [Fact] void should_not_store_an_orphan_child() => _childrenAfterFailure.ShouldBeEmpty();
    [Fact] void should_store_one_child_on_retry() => _childrenAfterRetry.Length.ShouldEqual(1);
    [Fact] void should_preserve_the_creation_field_on_retry() => _childrenAfterRetry[0]["name"].ShouldEqual("Child");
    [Fact] void should_store_the_changed_email_on_retry() => _childrenAfterRetry[0]["email"].ShouldEqual("updated@example.com");
    [Fact] void should_acknowledge_both_after_retry() => _remainingAfterRetry.ShouldEqual(0);
}
