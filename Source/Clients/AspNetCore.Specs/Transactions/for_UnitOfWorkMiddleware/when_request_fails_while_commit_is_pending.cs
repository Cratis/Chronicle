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

public class when_request_fails_while_commit_is_pending : Specification
{
    readonly CorrelationId _correlationId = CorrelationId.New();
    readonly DefaultHttpContext _context = new();
    readonly Exception _requestError = new InvalidOperationException("request failed");
    TaskCompletionSource<AppendManyResult> _append;
    UnitOfWork _unit;
    IUnitOfWorkManager _manager;
    ICorrelationIdAccessor _correlationIds;
    Exception _error;
    Task _commit;

    void Establish()
    {
        _append = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var store = Substitute.For<IEventStore>();
        var sequence = Substitute.For<IEventSequence>();
        store.GetEventSequence(EventSequenceId.Log).Returns(sequence);
        sequence.AppendMany(
            Arg.Any<IEnumerable<EventForEventSourceId>>(),
            Arg.Any<CorrelationId?>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>()).Returns(_ => _append.Task);
        _unit = new UnitOfWork(_correlationId, _ => { }, store, UnitOfWorkLifecyclePolicy.Strict);
        _manager = Substitute.For<IUnitOfWorkManager>();
        _manager.Begin(_correlationId).Returns(_unit);
        _correlationIds = Substitute.For<ICorrelationIdAccessor>();
        _correlationIds.Current.Returns(_correlationId);
    }

    async Task Because()
    {
        var middleware = new UnitOfWorkMiddleware(
            _ =>
            {
                _unit.AddEvent(EventSequenceId.Log, "original", new object(), Causation.Unknown());
                _commit = _unit.Commit();
                throw _requestError;
            },
            Substitute.For<ILogger<UnitOfWorkMiddleware>>());
        _error = await Catch.Exception(() => middleware.InvokeAsync(_context, _manager, _correlationIds));
        _append.SetResult(AppendManyResult.Success(_correlationId, []));
        await _commit;
    }

    [Fact] void should_propagate_the_original_request_failure() => _error.ShouldEqual(_requestError);
    [Fact] void should_not_discard_staged_events() => _unit.GetEvents().Count().ShouldEqual(1);
    [Fact] void should_restore_the_request_feature() => _context.Features.Get<IUnitOfWorkCompletionFeature>().ShouldBeNull();
}
