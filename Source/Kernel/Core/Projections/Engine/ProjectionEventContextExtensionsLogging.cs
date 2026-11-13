// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Log messages for <see cref="ProjectionEventContextExtensions"/>.
/// </summary>
internal static partial class ProjectionEventContextExtensionsLogging
{
    [LoggerMessage(LogLevel.Error, "Error when trying to resolve join")]
    internal static partial void ResolveJoinFailed(this ILogger logger, Exception error);
}
