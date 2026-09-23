// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Log messages for <see cref="ReadModelReleaser"/>.
/// </summary>
internal static partial class ReadModelReleaserLogMessages
{
    [LoggerMessage(LogLevel.Warning, "Read model '{ReadModelType}' has only subject-scoped compliance/security metadata but no subject could be resolved; returning its values without releasing them")]
    internal static partial void NoSubjectForRelease(this ILogger logger, string readModelType);

    [LoggerMessage(LogLevel.Debug, "Read model '{ReadModelType}' has no resolvable subject (no [Subject] or Id property); releasing against Subject.NotSet, which only reaches its namespace- or global-scoped values")]
    internal static partial void NoResolvableSubjectFallingBackToNotSet(this ILogger logger, string readModelType);

    [LoggerMessage(LogLevel.Error, "Failed to release compliance or security metadata for read model '{ReadModelType}' with subject '{Subject}': {Error}")]
    internal static partial void FailedToRelease(this ILogger logger, string readModelType, string subject, string error);
}
