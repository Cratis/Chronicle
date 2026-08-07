// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Log messages for <see cref="ReadModelReleaser"/>.
/// </summary>
internal static partial class ReadModelReleaserLogMessages
{
    [LoggerMessage(LogLevel.Error, "Failed to release compliance for read model '{ReadModelType}' with subject '{Subject}': {Error}")]
    internal static partial void FailedToRelease(this ILogger logger, string readModelType, string subject, string error);

    [LoggerMessage(LogLevel.Warning, "Read model '{ReadModelType}' declares '{Properties}' released under '{SubjectProperty}', which holds no subject. The values are returned as they were read.")]
    internal static partial void DeclaredReleaseSubjectNotResolved(this ILogger logger, string readModelType, string properties, string subjectProperty);
}
