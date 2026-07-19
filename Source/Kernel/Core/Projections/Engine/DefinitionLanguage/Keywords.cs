// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage;

/// <summary>
/// Defines the reserved keywords for the Projection Declaration Language.
/// </summary>
public static class Keywords
{
    /// <summary>
    /// Gets the set of all reserved keywords.
    /// </summary>
    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        "projection",
        "sequence",
        "every",
        "from",
        "key",
        "parent",
        "on",
        "with",
        "join",
        "events",
        "children",
        "id",
        "identified",
        "remove",
        "via",
        "automap",
        "no",
        "exclude",
        "increment",
        "decrement",
        "count",
        "add",
        "subtract",
        "by",
        "true",
        "false",
        "null",
        "literal",
        "all",
        "nested",
        "clear"
    };
}
