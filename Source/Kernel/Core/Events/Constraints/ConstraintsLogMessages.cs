// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Holds log messages for <see cref="Constraints"/>.
/// </summary>
internal static partial class ConstraintsLogMessages
{
    [LoggerMessage(LogLevel.Warning, "Failed publishing changed constraints for event store {EventStore}")]
    internal static partial void FailedPublishingConstraintsChanged(this ILogger<Constraints> logger, EventStoreName eventStore, Exception exception);

    [LoggerMessage(LogLevel.Error, "Failed starting the rebuild of stale constraint indexes for event store {EventStore}; the registered definitions are persisted, but their indexes were not rebuilt")]
    internal static partial void FailedStartingRebuildOfStaleIndexes(this ILogger<Constraints> logger, EventStoreName eventStore, Exception exception);
}
