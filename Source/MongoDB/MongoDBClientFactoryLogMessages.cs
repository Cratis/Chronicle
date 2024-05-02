// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.MongoDB;

/// <summary>
/// Holds all log messages for <see cref="MongoDBClientFactory"/>.
/// </summary>
internal static partial class MongoDBClientFactoryLogMessages
{
    [LoggerMessage(0, LogLevel.Trace, "Command ({RequestId}) '{CommandName}' started. Details : {Command}")]
    internal static partial void CommandStarted(this ILogger<MongoDBClientFactory> logger, int requestId, string commandName, string command);

    [LoggerMessage(1, LogLevel.Error, "Command ({RequestId}) '{CommandName}' failed with '{failure}'")]
    internal static partial void CommandFailed(this ILogger<MongoDBClientFactory> logger, int requestId, string commandName, string failure);

    [LoggerMessage(2, LogLevel.Trace, "Command ({RequestId}) '{CommandName}' succeeded.")]
    internal static partial void CommandSucceeded(this ILogger<MongoDBClientFactory> logger, int requestId, string commandName);

    [LoggerMessage(3, LogLevel.Trace, "Creating MongoClient for connecting to '{Address}'")]
    internal static partial void CreateClient(this ILogger<MongoDBClientFactory> logger, string address);
}
