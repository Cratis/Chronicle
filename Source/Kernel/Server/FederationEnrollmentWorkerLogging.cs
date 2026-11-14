// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server;

/// <summary>
/// Log messages of the federation enrollment worker.
/// </summary>
internal static partial class FederationEnrollmentWorkerLogging
{
    [LoggerMessage(LogLevel.Warning, "The federation authority is not ready yet, will retry. {ExceptionType}: {Reason}")]
    internal static partial void AuthorityNotReady(this ILogger<FederationEnrollmentWorker> logger, string exceptionType, string reason);

    [LoggerMessage(LogLevel.Warning, "Federation enrollment is not ready yet, will retry. {ExceptionType}: {Reason}")]
    internal static partial void EnrollmentNotReady(this ILogger<FederationEnrollmentWorker> logger, string exceptionType, string reason);
}
