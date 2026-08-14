// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Log messages for <see cref="ReadModelReleaser"/>.
/// </summary>
internal static partial class ReadModelReleaserLogMessages
{
    [LoggerMessage(LogLevel.Warning, "Read model '{ReadModelType}' has compliance metadata but no subject could be resolved; returning its values without releasing them")]
    internal static partial void NoSubjectForRelease(this ILogger logger, string readModelType);

    [LoggerMessage(LogLevel.Error, "Failed to release compliance for read model '{ReadModelType}' with subject '{Subject}': {Error}")]
    internal static partial void FailedToRelease(this ILogger logger, string readModelType, string subject, string error);
}
