// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ProtectedValues;

internal static partial class UnrecoverableProtectedValueReporterLogMessages
{
    [LoggerMessage(LogLevel.Error, "Unrecoverable protected value in {EventStore}/{Namespace} for {Identifier} at {PropertyPath} during {Phase}: {ExceptionType}, attempted revisions {AttemptedRevisions}.")]
    internal static partial void UnrecoverableProtectedValue(this ILogger logger, string eventStore, string @namespace, string identifier, string propertyPath, string phase, string exceptionType, int attemptedRevisions);
}
