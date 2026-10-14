// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Transactions;

internal static partial class UnitOfWorkLogging
{
    [LoggerMessage(LogLevel.Error, "Late event staging was attempted after unit of work '{CorrelationId}' began completing; these events will not be appended by this unit of work.")]
    internal static partial void LateStagingAttemptedAfterCompletion(this ILogger<UnitOfWork> logger, CorrelationId correlationId);

    [LoggerMessage(LogLevel.Debug, "keyguard.implicit_after_completion: unit '{CorrelationId}' ignored an implicit scope.")]
    internal static partial void ImplicitScopeAfterCompletion(this ILogger<UnitOfWork> logger, CorrelationId correlationId);

    [LoggerMessage(LogLevel.Debug, "keyguard.other_sequence: unit '{CorrelationId}' ignored {Count} implicit scopes on another sequence.")]
    internal static partial void ImplicitScopesOnOtherSequence(this ILogger<UnitOfWork> logger, CorrelationId correlationId, int count);
}
