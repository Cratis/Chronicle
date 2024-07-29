// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle;

internal static partial class EventStoreLogMessages
{
    [LoggerMessage(LogLevel.Trace, "Discover all artifacts")]
    internal static partial void DiscoverAllArtifacts(this ILogger logger);

    [LoggerMessage(LogLevel.Trace, "Register all artifacts")]
    internal static partial void RegisterAllArtifacts(this ILogger logger);
}
