// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.AspNetCore.Transactions;

/// <summary>Builds a real unit of work over a substituted event sequence, hosted by the real middleware.</summary>
internal sealed class UnitOfWorkHarness
{
    internal CorrelationId CorrelationId { get; } = CorrelationId.New();

    internal DefaultHttpContext Context { get; } = new();

    internal UnitOfWork Unit { get; }

    internal IEventSequence Sequence { get; } = Substitute.For<IEventSequence>();

    internal IUnitOfWorkManager Manager { get; } = Substitute.For<IUnitOfWorkManager>();

    internal ICorrelationIdAccessor CorrelationIds { get; } = Substitute.For<ICorrelationIdAccessor>();

    internal ILogger<UnitOfWorkMiddleware> Logger { get; } = Substitute.For<ILogger<UnitOfWorkMiddleware>>();

    internal UnitOfWorkHarness(AppendManyResult? result = null)
    {
        var store = Substitute.For<IEventStore>();
        store.Name.Returns((EventStoreName)"store");
        store.Namespace.Returns((EventStoreNamespaceName)"namespace");
        store.GetEventSequence(EventSequenceId.Log).Returns(Sequence);
        Unit = new UnitOfWork(CorrelationId, _ => { }, store);
        Manager.Begin(CorrelationId).Returns(Unit);
        CorrelationIds.Current.Returns(CorrelationId);
        Logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        AppendReturns(result ?? AppendManyResult.Success(CorrelationId, []));
    }

    internal void AppendReturns(AppendManyResult result) => Sequence.AppendMany(
        Arg.Any<IEnumerable<EventForEventSourceId>>(),
        Arg.Any<CorrelationId?>(),
        Arg.Any<IEnumerable<string>>(),
        Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>()).Returns(result);

    internal void AppendThrows(Exception exception) => Sequence.AppendMany(
        Arg.Any<IEnumerable<EventForEventSourceId>>(),
        Arg.Any<CorrelationId?>(),
        Arg.Any<IEnumerable<string>>(),
        Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>()).Returns<AppendManyResult>(_ => throw exception);

    internal void AddEvent() => Unit.AddEvent(EventSequenceId.Log, "source", new object(), Causation.Unknown());

    internal Task Run(RequestDelegate action) =>
        new UnitOfWorkMiddleware(action, Logger).InvokeAsync(Context, Manager, CorrelationIds);

    internal int AppendCalls => Sequence.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(IEventSequence.AppendMany));

    internal int LogCalls(LogLevel level) => Logger.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(ILogger.Log) && (LogLevel)_.GetArguments()[0]! == level);
}
