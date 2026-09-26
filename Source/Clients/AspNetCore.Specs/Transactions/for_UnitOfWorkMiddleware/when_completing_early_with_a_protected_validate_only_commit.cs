// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkMiddleware;

public class when_completing_early_with_a_protected_validate_only_commit : Specification
{
    readonly CorrelationId _correlationId = CorrelationId.New();
    readonly DefaultHttpContext _context = new();
    UnitOfWork _unit;
    IUnitOfWork _completed;
    IEventSequence _sequence;
    IUnitOfWorkManager _manager;
    ICorrelationIdAccessor _correlationIds;
    IEnumerable<EventForEventSourceId> _eventsAppended;
    IDictionary<EventSourceId, ConcurrencyScope> _scopesAppended;
    bool _succeededBeforeResponse;

    void Establish()
    {
        var store = Substitute.For<IEventStore>();
        store.Name.Returns((EventStoreName)"store");
        store.Namespace.Returns((EventStoreNamespaceName)"namespace");
        _sequence = Substitute.For<IEventSequence>();
        store.GetEventSequence(EventSequenceId.Log).Returns(_sequence);
        _unit = new UnitOfWork(_correlationId, _ => { }, store);
        _manager = Substitute.For<IUnitOfWorkManager>();
        _manager.Begin(_correlationId).Returns(_unit);
        _correlationIds = Substitute.For<ICorrelationIdAccessor>();
        _correlationIds.Current.Returns(_correlationId);
        _sequence.AppendMany(
            Arg.Any<IEnumerable<EventForEventSourceId>>(),
            Arg.Any<CorrelationId?>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>())
            .Returns(call =>
            {
                _eventsAppended = call.Arg<IEnumerable<EventForEventSourceId>>();
                _scopesAppended = call.Arg<IDictionary<EventSourceId, ConcurrencyScope>>();
                return AppendManyResult.Success(_correlationId, []);
            });
    }

    async Task Because()
    {
        var middleware = new UnitOfWorkMiddleware(
            async context =>
            {
                _unit.AddDecisionRead(ProtectedRead.Create());
                _completed = await context.Features.Get<IUnitOfWorkCompletionFeature>()!.CommitAsync();
                _succeededBeforeResponse = _completed.IsSuccess;
            },
            Substitute.For<ILogger<UnitOfWorkMiddleware>>());
        await middleware.InvokeAsync(_context, _manager, _correlationIds);
    }

    [Fact] void should_return_the_completed_unit() => ReferenceEquals(_completed, _unit).ShouldBeTrue();
    [Fact] void should_report_success_before_responding() => _succeededBeforeResponse.ShouldBeTrue();
    [Fact] void should_validate_without_events() => _eventsAppended.ShouldBeEmpty();
    [Fact] void should_send_the_decision_scope() => _scopesAppended["source"].SequenceNumber.ShouldEqual((EventSequenceNumber)4);
    [Fact] void should_not_commit_again() => _sequence.Received(1).AppendMany(
        Arg.Any<IEnumerable<EventForEventSourceId>>(),
        Arg.Any<CorrelationId?>(),
        Arg.Any<IEnumerable<string>>(),
        Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>());
    [Fact] void should_remove_the_request_feature() => _context.Features.Get<IUnitOfWorkCompletionFeature>().ShouldBeNull();
}
