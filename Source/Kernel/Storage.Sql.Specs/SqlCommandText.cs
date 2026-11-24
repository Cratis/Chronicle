// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Inspects SQL command text sent by Entity Framework Core, ignoring provider preambles.
/// </summary>
public static class SqlCommandText
{
    const string NoCountPrefix = "SET NOCOUNT ";

    /// <summary>
    /// Checks whether the command text starts with the given statement verb, skipping leading whitespace and any SQL Server SET NOCOUNT preamble.
    /// </summary>
    /// <param name="commandText">The command text to inspect.</param>
    /// <param name="verb">The statement verb, such as DELETE or UPDATE.</param>
    /// <returns>True if the first statement starts with the verb; false otherwise.</returns>
    public static bool StartsWithStatement(string commandText, string verb)
    {
        var text = commandText.AsSpan().TrimStart();
        while (text.StartsWith(NoCountPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var end = text.IndexOf(';');
            if (end < 0) return false;
            text = text[(end + 1)..].TrimStart();
        }

        return text.StartsWith(verb, StringComparison.OrdinalIgnoreCase);
    }
}
