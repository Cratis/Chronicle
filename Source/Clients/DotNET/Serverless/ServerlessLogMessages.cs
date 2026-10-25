// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Serverless;

internal static partial class ServerlessLogMessages
{
    [LoggerMessage(Message = "Serverless mode is on. What it gives up: {Downgrades}")]
    internal static partial void ServerlessDowngrades(this ILogger logger, LogLevel level, string downgrades);

    [LoggerMessage(LogLevel.Warning, "The host looks like a function host ({Variable} is set) but serverless mode is off, so {StreamObservers} observer(s) will open streams. Set Cratis:Chronicle:Serverless:Mode to Serverless if this process is short-lived")]
    internal static partial void ServerlessHostDetected(this ILogger logger, string variable, int streamObservers);

    [LoggerMessage(LogLevel.Warning, "Serverless observer delivery overrides match no discovered observer: {ObserverIds}")]
    internal static partial void UnknownObserverDeliveryOverrides(this ILogger logger, string observerIds);
}
