// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace Aksio.Cratis.Extensions.MongoDB
{
    /// <summary>
    /// Holds all log messages for <see cref="MongoDBClientFactory"/>.
    /// </summary>
    public static partial class MongoDBClientFactoryLogMessages
    {
        [LoggerMessage(0, LogLevel.Trace, "Command ({RequestId}) '{CommandName}' started with content {Command}.")]
        public static partial void CommandStarted(this ILogger logger, int requestId, string commandName, BsonDocument command);

        [LoggerMessage(1, LogLevel.Error, "Command ({RequestId}) '{CommandName}' failed.")]
        public static partial void CommandFailed(this ILogger logger, int requestId, string commandName, Exception ex);

        [LoggerMessage(2, LogLevel.Trace, "Command ({RequestId}) '{CommandName}' succeeded.")]
        public static partial void CommandSucceeded(this ILogger logger, int requestId, string commandName);
    }
}
