// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage;

/// <summary>
/// Defines the reserved keywords for the Capture Declaration Language.
/// </summary>
public static class Keywords
{
    /// <summary>
    /// Gets the mapping of keyword strings to their corresponding token types.
    /// </summary>
    public static readonly IDictionary<string, TokenType> TokenMapping;

    static Keywords()
    {
        TokenMapping = new Dictionary<string, TokenType>(StringComparer.Ordinal)
        {
            { "capture", TokenType.Capture },
            { "source", TokenType.Source },
            { "key", TokenType.Key },
            { "map", TokenType.Map },
            { "append", TokenType.Append },
            { "when", TokenType.When },
            { "nested", TokenType.Nested },
            { "children", TokenType.Children },
            { "identified", TokenType.Identified },
            { "by", TokenType.By },
            { "api", TokenType.Api },
            { "webhook", TokenType.Webhook },
            { "message", TokenType.Message },
            { "poll", TokenType.Poll },
            { "auth", TokenType.Auth },
            { "route", TokenType.Route },
            { "path", TokenType.Path },
            { "topic", TokenType.Topic },
            { "from", TokenType.From },
            { "to", TokenType.To },
            { "or", TokenType.Or },
            { "and", TokenType.And },
            { "added", TokenType.Added },
            { "removed", TokenType.Removed },
            { "translate", TokenType.Translate },
            { "split", TokenType.Split },
            { "bearer", TokenType.Bearer },
            { "true", TokenType.True },
            { "false", TokenType.False },
            { "null", TokenType.Null }
        };
    }
}
