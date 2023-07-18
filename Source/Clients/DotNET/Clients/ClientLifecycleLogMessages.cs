// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Clients;

internal static partial class ClientLifecycleLogMessages
{
    [LoggerMessage(1, LogLevel.Error, "During the connected lifecycle event, the participant '{Participant}' failed")]
    internal static partial void ParticipantFailedDuringConnected(this ILogger<ConnectionLifecycle> logger, string participant, Exception exception);

    [LoggerMessage(2, LogLevel.Error, "During the disconnected lifecycle event, the participant '{Participant}' failed")]
    internal static partial void ParticipantFailedDuringDisconnected(this ILogger<ConnectionLifecycle> logger, string participant, Exception exception);
}
