// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.AspNetCore.Transactions;

internal static partial class UnitOfWorkMiddlewareLogging
{
    [LoggerMessage(LogLevel.Warning, "The unit of work for correlation {CorrelationId} has already been completed manually either by Commit or Rollback. It is recommended to not complete the unit of work manually in this context.")]
    internal static partial void AlreadyCompletedManually(this ILogger<UnitOfWorkMiddleware> logger, CorrelationId correlationId);

    [LoggerMessage(LogLevel.Error, "Committing the unit of work for correlation {CorrelationId} failed after the request ran (response started: {ResponseStarted}): {ConcurrencyViolations} concurrency violations, {DecisionConflicts} decision conflicts, {ConstraintViolations} constraint violations, {AppendErrors} append errors. Claim completion through IUnitOfWorkCompletionOwnershipFeature to report the outcome to the caller.")]
    internal static partial void CommitFailedAfterResponse(this ILogger<UnitOfWorkMiddleware> logger, CorrelationId correlationId, bool responseStarted, int concurrencyViolations, int decisionConflicts, int constraintViolations, int appendErrors);

    [LoggerMessage(LogLevel.Error, "The unit of work for correlation {CorrelationId} was claimed but never completed by its owner. It has been rolled back.")]
    internal static partial void ClaimedButNotCompleted(this ILogger<UnitOfWorkMiddleware> logger, CorrelationId correlationId);
}
