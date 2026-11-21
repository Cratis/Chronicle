// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ProjectionEditor;

/// <summary>
/// Log messages for <see cref="GraphProjectionPreviewer"/>.
/// </summary>
internal static partial class GraphProjectionPreviewerLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Event {EventSequenceNumber} could not be applied to the graph preview")]
    internal static partial void EventCouldNotBeApplied(this ILogger<GraphProjectionPreviewer> logger, EventSequenceNumber eventSequenceNumber, Exception exception);
}
