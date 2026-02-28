// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Services.Clients;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class ConnectionServiceLogMessages
{
    [LoggerMessage(LogLevel.Error, "Failure during keep alive for connection {ConnectionId}")]
    internal static partial void FailureDuringKeepAlive(this ILogger<ConnectionService> logger, string connectionId, Exception exception);
}
