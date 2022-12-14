// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Extensions.MongoDB;

/// <summary>
/// Holds all log messages for <see cref="MongoDBClientFactory"/>.
/// </summary>
public static partial class MongoDBClientFactoryLogMessages
{
    [LoggerMessage(0, LogLevel.Trace, "Command ({RequestId}) '{CommandName}' started. Details : {Command}")]
    public static partial void CommandStarted(this ILogger logger, int requestId, string commandName, string command);

    [LoggerMessage(1, LogLevel.Error, "Command ({RequestId}) '{CommandName}' failed with '{failure}'")]
    public static partial void CommandFailed(this ILogger logger, int requestId, string commandName, string failure);

    [LoggerMessage(2, LogLevel.Trace, "Command ({RequestId}) '{CommandName}' succeeded.")]
    public static partial void CommandSucceeded(this ILogger logger, int requestId, string commandName);

    [LoggerMessage(3, LogLevel.Trace, "Creating MongoClient for connecting to '{Address}'")]
    public static partial void CreateClient(this ILogger logger, string address);
}
