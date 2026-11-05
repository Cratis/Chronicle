// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Produces the text used in logs and failure reasons for MongoDB write errors.
/// </summary>
internal static class WriteErrorText
{
    const string IndexMarker = "index: ";

    /// <summary>
    /// Gets the message to report for a write error. A duplicate key message carries the stored key value, so it is replaced by a neutral text.
    /// </summary>
    /// <param name="category">The error category.</param>
    /// <param name="message">The message from the server.</param>
    /// <returns>The message that is safe to log and report.</returns>
    internal static string For(ServerErrorCategory category, string message)
    {
        if (category != ServerErrorCategory.DuplicateKey)
        {
            return message;
        }

        var start = message.IndexOf(IndexMarker, StringComparison.Ordinal);
        if (start < 0)
        {
            return "duplicate key";
        }

        start += IndexMarker.Length;
        var end = message.IndexOf(' ', start);
        var index = end < 0 ? message[start..] : message[start..end];
        return $"duplicate key in index {index}";
    }
}
