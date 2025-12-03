// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.AspNetCore.Transactions;

internal static partial class UnitOfWorkMiddlewareLogging
{
    [LoggerMessage(LogLevel.Warning, "The unit of work for correlation {CorrelationId} has already been completed manually either by Commit or Rollback. It is recommended to not complete the unit of work manually in this context.")]
    internal static partial void AlreadyCompletedManually(this ILogger<UnitOfWorkMiddleware> logger, CorrelationId correlationId);
}
