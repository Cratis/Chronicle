// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.DefinitionLanguage;

/// <summary>
/// Defines the reserved keywords for the Projection Definition Language.
/// </summary>
public static class Keywords
{
    /// <summary>
    /// Gets the set of all reserved keywords (case-insensitive).
    /// </summary>
    public static readonly IReadOnlySet<string> All;

    /// <summary>
    /// Gets the mapping of keyword strings to their corresponding token types.
    /// </summary>
    public static readonly IDictionary<string, TokenType> TokenMapping;

    static Keywords()
    {
        TokenMapping = new Dictionary<string, TokenType>(StringComparer.Ordinal)
        {
            { "projection", TokenType.Projection },
            { "sequence", TokenType.Sequence },
            { "every", TokenType.Every },
            { "from", TokenType.From },
            { "key", TokenType.Key },
            { "parent", TokenType.Parent },
            { "on", TokenType.On },
            { "with", TokenType.With },
            { "join", TokenType.Join },
            { "events", TokenType.Events },
            { "children", TokenType.Children },
            { "id", TokenType.Id },
            { "identified", TokenType.Identified },
            { "remove", TokenType.Remove },
            { "via", TokenType.Via },
            { "automap", TokenType.AutoMap },
            { "no", TokenType.No },
            { "exclude", TokenType.Exclude },
            { "increment", TokenType.Increment },
            { "decrement", TokenType.Decrement },
            { "count", TokenType.Count },
            { "add", TokenType.Add },
            { "subtract", TokenType.Subtract },
            { "by", TokenType.By },
            { "true", TokenType.True },
            { "false", TokenType.False },
            { "null", TokenType.Null }
        };

        All = new HashSet<string>(TokenMapping.Keys, StringComparer.Ordinal);
    }
}
