// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkMiddleware;

public class when_staging_after_strict_completion : Specification
{
    readonly CorrelationId _correlationId = CorrelationId.New();
    readonly DefaultHttpContext _context = new();
    UnitOfWork _unit;
    IUnitOfWorkManager _manager;
    ICorrelationIdAccessor _correlationIds;
    Exception _error;

    void Establish()
    {
        var store = Substitute.For<IEventStore>();
        _unit = new UnitOfWork(_correlationId, _ => { }, store, UnitOfWorkLifecyclePolicy.Strict);
        _manager = Substitute.For<IUnitOfWorkManager>();
        _manager.Begin(_correlationId).Returns(_unit);
        _correlationIds = Substitute.For<ICorrelationIdAccessor>();
        _correlationIds.Current.Returns(_correlationId);
    }

    async Task Because()
    {
        var middleware = new UnitOfWorkMiddleware(
            async _ =>
            {
                await _unit.Commit();
                _unit.AddEvent(EventSequenceId.Log, "late", new object(), Causation.Unknown());
            },
            Substitute.For<ILogger<UnitOfWorkMiddleware>>());
        _error = await Catch.Exception(() => middleware.InvokeAsync(_context, _manager, _correlationIds));
    }

    [Fact] void should_propagate_the_lifecycle_error() => _error.ShouldBeOfExactType<UnitOfWorkIsCompleted>();
    [Fact] void should_restore_the_request_feature() => _context.Features.Get<IUnitOfWorkCompletionFeature>().ShouldBeNull();
}
