// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle;

internal static partial class ArtifactActivatorLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Activating artifact of type '{ArtifactType}'")]
    internal static partial void ActivatingArtifact(this ILogger logger, Type artifactType);

    [LoggerMessage(LogLevel.Warning, "Failed to activate artifact of type '{ArtifactType}'")]
    internal static partial void ArtifactActivationFailed(this ILogger logger, Type artifactType, Exception exception);

    [LoggerMessage(LogLevel.Debug, "Disposing artifact of type '{ArtifactType}'")]
    internal static partial void DisposingArtifact(this ILogger logger, Type artifactType);

    [LoggerMessage(LogLevel.Warning, "Failed to dispose artifact of type '{ArtifactType}'")]
    internal static partial void FailedDisposingArtifact(this ILogger logger, Type artifactType, Exception exception);
}
