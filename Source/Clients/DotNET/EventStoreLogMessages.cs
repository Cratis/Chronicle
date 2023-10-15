// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis;

internal static partial class EventStoreLogMessages
{
    [LoggerMessage(0, LogLevel.Trace, "Discover all artifacts")]
    internal static partial void DiscoverAllArtifacts(this ILogger logger);
}
