// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Storage.Hosts;

/// <summary>Names the kind of a SQL command by its first word, so SQL counts group like MongoDB command names do.</summary>
internal static class SqlVerb
{
    /// <summary>The name recorded for a transaction commit.</summary>
    public const string Commit = "commit";

    /// <summary>The name recorded for a transaction rollback (the counterpart of MongoDB's abortTransaction).</summary>
    public const string Rollback = "rollback";

    /// <summary>The name recorded for a savepoint operation.</summary>
    public const string Savepoint = "savepoint";

    /// <summary>Gets the verb a command text starts with.</summary>
    /// <param name="text">The command text.</param>
    /// <returns>The lower-case verb, or `other`.</returns>
    public static string Of(string? text)
    {
        var trimmed = (text ?? string.Empty).AsSpan().TrimStart();
        var end = 0;
        while (end < trimmed.Length && char.IsLetter(trimmed[end])) end++;
        return end == 0 ? "other" : trimmed[..end].ToString().ToLowerInvariant();
    }
}
