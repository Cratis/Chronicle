// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveFutures;

public class when_a_future_fails_repeatedly : given.a_resolve_futures_step
{
    readonly Exception _failure = new("Temporary projection failure");
    ProjectionFuture _future;
    int _attempts;

    void Establish()
    {
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        var child = Substitute.For<IProjection>();
        child.Parent.Returns(_projection);
        child.Path.Returns(new ProjectionPath("children"));
        child.ChildrenPropertyPath.Returns(new PropertyPath("children"));
        child.OnNext(Arg.Do<ProjectionEventContext>(context =>
        {
            if (++_attempts <= 2) throw _failure;
            context.Changeset.AddChild<ExpandoObject>(new PropertyPath("children"), new PropertyPath("id"), "child-key", [], context.Key.ArrayIndexers);
        }));
        _projection.ChildProjections.Returns([child]);
        _future = new ProjectionFuture(
            ProjectionFutureId.New(),
            _projection.Identifier,
            _event,
            PropertyPath.Root,
            new PropertyPath("children"),
            new PropertyPath("id"),
            new PropertyPath("id"),
            new Key("parent-key", ArrayIndexers.NoIndexers),
            DateTimeOffset.UtcNow);
        _projectionFutures.GetFutures().Returns(Task.FromResult<IEnumerable<ProjectionFuture>>([_future]));
        _tracker.HasPending = true;
        var state = new ExpandoObject();
        ((IDictionary<string, object?>)state)[WellKnownProperties.ReadModelInstanceInitialized] = true;
        _context = _context with
        {
            Key = _future.ParentKey,
            Changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), _event, state)
        };
    }

    async Task Because()
    {
        await _step.Perform(_projection, _context);
        await _step.Perform(_projection, _context);
        await _step.Perform(_projection, _context);
    }

    [Fact] void should_warn_only_for_the_first_failure() => FailureLogs(LogLevel.Warning).ShouldEqual(1);
    [Fact] void should_report_subsequent_failures_at_debug() => FailureLogs(LogLevel.Debug).ShouldEqual(1);
    [Fact] void should_allow_the_future_to_recover() => _context.PendingFutureSaves.Single().FutureId.ShouldEqual(_future.Id);
    [Fact] void should_not_discard_the_deferred_event() => _projectionFutures.DidNotReceive().ResolveFuture(_future.Id);

    int FailureLogs(LogLevel level) => _logger.ReceivedCalls().Count(call =>
        call.GetMethodInfo().Name == nameof(ILogger.Log) &&
        call.GetArguments()[0] is LogLevel actualLevel && actualLevel == level &&
        ReferenceEquals(call.GetArguments()[3], _failure));
}
