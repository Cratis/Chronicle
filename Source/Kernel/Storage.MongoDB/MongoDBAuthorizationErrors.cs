// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Identifies MongoDB authorization errors without inspecting exception messages.
/// </summary>
public static class MongoDBAuthorizationErrors
{
    /// <summary>
    /// Determines whether an exception contains a refused MongoDB command.
    /// </summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <param name="command">The command name, without its arguments.</param>
    /// <returns>Whether a MongoDB command failed with code 13.</returns>
    public static bool IsUnauthorized(Exception exception, out string? command)
    {
        if (exception is MongoCommandException { Code: 13 } mongo)
        {
            command = mongo.Command is { ElementCount: > 0 } document ? document.GetElement(0).Name : null;
            return true;
        }

        if (exception is AggregateException aggregate)
        {
            foreach (var inner in aggregate.InnerExceptions)
            {
                if (IsUnauthorized(inner, out command)) return true;
            }
        }
        else if (exception.InnerException is { } inner && IsUnauthorized(inner, out command))
        {
            return true;
        }

        command = null;
        return false;
    }
}
