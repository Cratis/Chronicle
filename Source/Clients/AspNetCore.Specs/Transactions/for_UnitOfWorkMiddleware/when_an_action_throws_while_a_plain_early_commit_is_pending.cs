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

public class when_an_action_throws_while_a_plain_early_commit_is_pending : Specification
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
    bool _rolledBackBeforeAppendFinished;
    int _completionCount;

    void Establish()
    {
        var store = Substitute.For<IEventStore>();
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
                _unit.AddEvent(EventSequenceId.Log, "source", new object(), Causation.Unknown());
                _earlyCommit = context.Features.Get<IUnitOfWorkCompletionFeature>().CommitAsync();
                throw _actionError;
            },
            Substitute.For<ILogger<UnitOfWorkMiddleware>>());
        _middlewareError = await Record.ExceptionAsync(() => middleware.InvokeAsync(_context, _manager, _correlationIds));
        _rolledBackBeforeAppendFinished = _unit.IsCompleted;
        _append.SetResult(AppendManyResult.Success(_correlationId, []));
        await _earlyCommit;
    }

    [Fact] void should_preserve_the_action_exception() => ReferenceEquals(_middlewareError, _actionError).ShouldBeTrue();
    [Fact] void should_not_rollback_an_owner_commit_in_flight() => _rolledBackBeforeAppendFinished.ShouldBeFalse();
    [Fact] void should_complete_once_after_append_finishes() => _completionCount.ShouldEqual(1);
    [Fact] void should_remove_the_request_feature() => _context.Features.Get<IUnitOfWorkCompletionFeature>().ShouldBeNull();
}
