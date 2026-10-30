// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkMiddleware;

public class when_the_request_is_aborted : Specification
{
    readonly UnitOfWorkHarness _harness = new();
    Exception _middlewareError;

    void Establish() => _harness.Context.RequestAborted = new CancellationToken(canceled: true);

    async Task Because() => _middlewareError = await Record.ExceptionAsync(() => _harness.Run(context =>
    {
        _harness.AddEvent();
        context.RequestAborted.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }));

    [Fact] void should_let_the_cancellation_through() => _middlewareError.ShouldBeOfExactType<OperationCanceledException>();
    [Fact] void should_not_commit() => _harness.AppendCalls.ShouldEqual(0);
    [Fact] void should_complete_the_unit() => _harness.Unit.IsCompleted.ShouldBeTrue();
    [Fact] void should_discard_the_staged_events() => _harness.Unit.GetEvents().ShouldBeEmpty();
}
