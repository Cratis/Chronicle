// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactions;

internal static partial class ReactionsLogMessages
{
    [LoggerMessage(LogLevel.Information, "Registering reaction '{Id}'")]
    internal static partial void RegisteringReaction(this ILogger<Reactions> logger, ReactionId id);

    [LoggerMessage(LogLevel.Trace, "Discover all reactions")]
    internal static partial void DiscoverAllReactions(this ILogger<Reactions> logger);

    [LoggerMessage(LogLevel.Trace, "Event of type {EventTypeId} was received for reaction {ReactionId}")]
    internal static partial void EventReceived(this ILogger<Reactions> logger, EventTypeId eventTypeId, ReactionId reactionId);
}
