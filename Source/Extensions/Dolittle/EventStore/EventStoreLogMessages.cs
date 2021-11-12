// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace Cratis.Extensions.Dolittle.EventStore
{
    /// <summary>
    /// Holds log messages for <see cref="EventStore"/>.
    /// </summary>
    public static partial class EventStoreLogMessages
    {
        [LoggerMessage(0, LogLevel.Trace, "MongoDB Started: {CommandName} - {Command}")]
        public static partial void MongoDBStarted(this ILogger logger, string commandName, BsonDocument command);

        [LoggerMessage(1, LogLevel.Trace, "MongoDB Succeeded: {CommandName} - {Reply}")]
        public static partial void MongoDBSucceeded(this ILogger logger, string commandName, BsonDocument reply);

        [LoggerMessage(2, LogLevel.Trace, "MongoDB Failed: {CommandName}")]
        public static partial void MongoDBFailed(this ILogger logger, string commandName, Exception failure);
    }
}
