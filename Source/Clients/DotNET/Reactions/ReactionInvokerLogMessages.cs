// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactions;

internal static partial class ReactionInvokerLogMessages
{
    [LoggerMessage(LogLevel.Error, "Reaction of type '{ReactionId}' failed for event with type '{EventType}'")]
    internal static partial void ReactionFailed(this ILogger<ReactionInvoker> logger, ReactionId reactionId, string eventType, Exception exception);
}
