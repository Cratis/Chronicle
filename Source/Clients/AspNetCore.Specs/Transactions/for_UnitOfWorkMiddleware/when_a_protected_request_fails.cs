// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkMiddleware;

public class when_a_protected_request_fails : Specification
{
    readonly CorrelationId _correlationId = CorrelationId.New();
    readonly DefaultHttpContext _context = new();
    UnitOfWork _unit;
    IEventSequence _sequence;
    Exception _error;
    bool _completedCallbackCalled;

    async Task Because()
    {
        var store = Substitute.For<IEventStore>();
        store.Name.Returns((EventStoreName)"store");
        store.Namespace.Returns((EventStoreNamespaceName)"namespace");
        _sequence = Substitute.For<IEventSequence>();
        store.GetEventSequence(EventSequenceId.Log).Returns(_sequence);
        _unit = new UnitOfWork(_correlationId, _ => _completedCallbackCalled = true, store);
        var manager = Substitute.For<IUnitOfWorkManager>();
        manager.Begin(_correlationId).Returns(_unit);
        var correlationIds = Substitute.For<ICorrelationIdAccessor>();
        correlationIds.Current.Returns(_correlationId);
        var middleware = new UnitOfWorkMiddleware(
            _ =>
            {
                _unit.AddDecisionRead(ProtectedRead.Create());
                throw new InvalidOperationException("request failed");
            },
            Substitute.For<ILogger<UnitOfWorkMiddleware>>());
        _error = await Record.ExceptionAsync(() => middleware.InvokeAsync(_context, manager, correlationIds));
    }

    [Fact] void should_preserve_the_request_exception() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_complete_the_owned_unit() => _unit.IsCompleted.ShouldBeTrue();
    [Fact] void should_call_completion() => _completedCallbackCalled.ShouldBeTrue();
    [Fact] void should_not_append() => _sequence.DidNotReceiveWithAnyArgs().AppendMany(default!);
    [Fact] void should_remove_the_request_feature() => _context.Features.Get<IUnitOfWorkCompletionFeature>().ShouldBeNull();
}
