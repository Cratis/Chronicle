// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Transactions;

internal static partial class UnitOfWorkLogging
{
    [LoggerMessage(LogLevel.Error, "Events were staged after unit of work '{CorrelationId}' began completing and will not be appended by this unit of work.")]
    internal static partial void EventsStagedAfterCompletion(this ILogger<UnitOfWork> logger, CorrelationId correlationId);
}
