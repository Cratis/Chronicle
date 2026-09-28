// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkMiddleware;

public class when_public_completion_races_a_failed_plain_action : Specification
{
    readonly CorrelationId _correlationId = CorrelationId.New();
    readonly InvalidOperationException _actionError = new("Action failed");
    Exception _rollbackError;
    Exception _disposeError;
    bool _rollbackCompleted;
    bool _disposeCompleted;

    async Task Because()
    {
        (_rollbackError, _rollbackCompleted) = await RunWithCompletionAtCheck(unit => unit.Rollback().GetAwaiter().GetResult());
        (_disposeError, _disposeCompleted) = await RunWithCompletionAtCheck(unit => unit.Dispose());
    }

    async Task<(Exception Error, bool Completed)> RunWithCompletionAtCheck(Action<UnitOfWork> publicCompletion)
    {
        var context = new DefaultHttpContext();
        var store = Substitute.For<IEventStore>();
        var completionCount = 0;
        var unit = new CompleteOnCheckUnit(_correlationId, _ => completionCount++, store, publicCompletion);
        var manager = Substitute.For<IUnitOfWorkManager>();
        manager.Begin(_correlationId).Returns(unit);
        var correlationIds = Substitute.For<ICorrelationIdAccessor>();
        correlationIds.Current.Returns(_correlationId);
        var middleware = new UnitOfWorkMiddleware(
            _ => throw _actionError,
            Substitute.For<ILogger<UnitOfWorkMiddleware>>());

        var error = await Record.ExceptionAsync(() => middleware.InvokeAsync(context, manager, correlationIds));
        return (error, unit.CompletionHappened && unit.IsCompleted && completionCount == 1 &&
            context.Features.Get<IUnitOfWorkCompletionFeature>() is null);
    }

    [Fact] void should_preserve_the_action_error_after_public_rollback() => ReferenceEquals(_rollbackError, _actionError).ShouldBeTrue();
    [Fact] void should_preserve_the_action_error_after_public_disposal() => ReferenceEquals(_disposeError, _actionError).ShouldBeTrue();
    [Fact] void should_complete_once_after_public_rollback() => _rollbackCompleted.ShouldBeTrue();
    [Fact] void should_complete_once_after_public_disposal() => _disposeCompleted.ShouldBeTrue();

    sealed class CompleteOnCheckUnit(
        CorrelationId correlationId,
        Action<IUnitOfWork> onCompleted,
        IEventStore store,
        Action<UnitOfWork> publicCompletion) : UnitOfWork(correlationId, onCompleted, store), IUnitOfWork
    {
        public bool CompletionHappened { get; private set; }

        bool IUnitOfWork.IsCompleted
        {
            get
            {
                // Return the pre-completion value while completing the plain claimed unit
                // before the middleware can use its owner capability to roll it back.
                var completed = IsCompleted;
                if (!completed)
                {
                    publicCompletion(this);
                    CompletionHappened = true;
                }
                return completed;
            }
        }
    }
}
