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

public class when_a_strict_action_throws_while_early_commit_is_pending : Specification
{
    readonly CorrelationId _correlationId = CorrelationId.New();
    readonly DefaultHttpContext _context = new();
    readonly InvalidOperationException _actionError = new("Action failed");
    TaskCompletionSource<AppendManyResult> _append;
    Task<IUnitOfWork> _earlyCommit;
    UnitOfWork _unit;
    IEventSequence _sequence;
    IUnitOfWorkCompletionFeature _previousFeature;
    Exception _middlewareError;
    bool _openWhilePending;
    int _completionCount;

    void Establish()
    {
        var store = Substitute.For<IEventStore>();
        store.Name.Returns((EventStoreName)"store");
        store.Namespace.Returns((EventStoreNamespaceName)"namespace");
        _sequence = Substitute.For<IEventSequence>();
        store.GetEventSequence(EventSequenceId.Log).Returns(_sequence);
        _unit = new UnitOfWork(_correlationId, _ => _completionCount++, store, UnitOfWorkLifecyclePolicy.Strict);
        _previousFeature = Substitute.For<IUnitOfWorkCompletionFeature>();
        _context.Features.Set(_previousFeature);
        _append = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _sequence.AppendMany(
            Arg.Any<IEnumerable<EventForEventSourceId>>(),
            Arg.Any<CorrelationId?>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>()).Returns(_ => _append.Task);
    }

    async Task Because()
    {
        var manager = Substitute.For<IUnitOfWorkManager>();
        manager.Begin(_correlationId).Returns(_unit);
        var correlationIds = Substitute.For<ICorrelationIdAccessor>();
        correlationIds.Current.Returns(_correlationId);
        var middleware = new UnitOfWorkMiddleware(
            context =>
            {
                _unit.AddDecisionRead(ProtectedRead.Create());
                _unit.AddEvent(EventSequenceId.Log, "source", new object(), Causation.Unknown());
                _earlyCommit = context.Features.Get<IUnitOfWorkCompletionFeature>().CommitAsync();
                throw _actionError;
            },
            Substitute.For<ILogger<UnitOfWorkMiddleware>>());
        _middlewareError = await Record.ExceptionAsync(() => middleware.InvokeAsync(_context, manager, correlationIds));
        _openWhilePending = !_unit.IsCompleted && _completionCount == 0 && _unit.GetEvents().Any();
        _append.SetResult(AppendManyResult.Success(_correlationId, []));
        await _earlyCommit;
    }

    [Fact] void should_preserve_the_original_exception() => ReferenceEquals(_middlewareError, _actionError).ShouldBeTrue();
    [Fact] void should_leave_the_in_flight_commit_alone() => _openWhilePending.ShouldBeTrue();
    [Fact] void should_complete_only_once() => _completionCount.ShouldEqual(1);
    [Fact] void should_restore_the_prior_feature() => ReferenceEquals(_context.Features.Get<IUnitOfWorkCompletionFeature>(), _previousFeature).ShouldBeTrue();
}
