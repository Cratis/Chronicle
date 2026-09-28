// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkMiddleware;

public class when_an_action_throws_while_an_early_commit_is_pending : Specification
{
    readonly CorrelationId _correlationId = CorrelationId.New();
    readonly DefaultHttpContext _context = new();
    readonly InvalidOperationException _actionError = new("Action failed");
    TaskCompletionSource<AppendManyResult> _append;
    Task<IUnitOfWork> _earlyCommit;
    UnitOfWork _unit;
    IEventSequence _sequence;
    IUnitOfWorkManager _manager;
    ICorrelationIdAccessor _correlationIds;
    Exception _middlewareError;
    bool _stillOpen;
    bool _eventsStillStaged;
    bool _callbackNotCalled;
    int _completionCount;

    void Establish()
    {
        var store = Substitute.For<IEventStore>();
        store.Name.Returns((EventStoreName)"store");
        store.Namespace.Returns((EventStoreNamespaceName)"namespace");
        _sequence = Substitute.For<IEventSequence>();
        store.GetEventSequence(EventSequenceId.Log).Returns(_sequence);
        _unit = new UnitOfWork(_correlationId, _ => _completionCount++, store);
        _manager = Substitute.For<IUnitOfWorkManager>();
        _manager.Begin(_correlationId).Returns(_unit);
        _correlationIds = Substitute.For<ICorrelationIdAccessor>();
        _correlationIds.Current.Returns(_correlationId);
        _append = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _sequence.AppendMany(
            Arg.Any<IEnumerable<EventForEventSourceId>>(),
            Arg.Any<CorrelationId?>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>()).Returns(_ => _append.Task);
    }

    async Task Because()
    {
        var middleware = new UnitOfWorkMiddleware(
            context =>
            {
                _unit.AddDecisionRead(ProtectedRead.Create());
                _unit.AddEvent(EventSequenceId.Log, "source", new object(), Causation.Unknown());
                _earlyCommit = context.Features.Get<IUnitOfWorkCompletionFeature>().CommitAsync();
                throw _actionError;
            },
            Substitute.For<ILogger<UnitOfWorkMiddleware>>());
        _middlewareError = await Record.ExceptionAsync(() => middleware.InvokeAsync(_context, _manager, _correlationIds));
        _stillOpen = !_unit.IsCompleted;
        _eventsStillStaged = _unit.GetEvents().Any();
        _callbackNotCalled = _completionCount == 0;
        _append.SetResult(AppendManyResult.Success(_correlationId, []));
        await _earlyCommit;
    }

    [Fact] void should_preserve_the_action_exception() => ReferenceEquals(_middlewareError, _actionError).ShouldBeTrue();
    [Fact] void should_not_report_a_rollback_before_append_finishes() => _stillOpen.ShouldBeTrue();
    [Fact] void should_keep_events_until_append_finishes() => _eventsStillStaged.ShouldBeTrue();
    [Fact] void should_not_call_completion_before_append_finishes() => _callbackNotCalled.ShouldBeTrue();
    [Fact] void should_commit_after_append_finishes() => _unit.IsSuccess.ShouldBeTrue();
    [Fact] void should_complete_only_once() => _completionCount.ShouldEqual(1);
    [Fact]
    void should_append_only_once() => _sequence.Received(1).AppendMany(
        Arg.Any<IEnumerable<EventForEventSourceId>>(),
        Arg.Any<CorrelationId?>(),
        Arg.Any<IEnumerable<string>>(),
        Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>());
    [Fact] void should_remove_the_request_feature() => _context.Features.Get<IUnitOfWorkCompletionFeature>().ShouldBeNull();
}
