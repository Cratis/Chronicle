// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage;

/// <summary>
/// Preserves numeric mapping tokens that Screenplay otherwise converts to double before visiting the syntax tree.
/// </summary>
internal static partial class NumericProjectionLiterals
{
    [GeneratedRegex(@"^(?<prefix>\s*(?:(?:add|subtract)\s+\S+\s+by\s+|set\s+\S+\s+(?:=|to)\s+|\S+\s*=\s*|(?:parent|key)\s+))(?<number>-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)\s*$", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    static partial Regex NumericMapping { get; }

    /// <summary>
    /// Replaces exponent tokens with a placeholder that the current Screenplay parser accepts; the original
    /// numeric tokens are restored from the source line when the parsed literal is stored in the definition.
    /// </summary>
    /// <param name="definition">The original projection declaration.</param>
    /// <returns>The parser input and the original numeric tokens by source line.</returns>
    internal static (string Input, IReadOnlyDictionary<int, string> Literals) Prepare(string definition)
    {
        var lines = definition.Split('\n');
        var literals = new Dictionary<int, string>();
        for (var index = 0; index < lines.Length; index++)
        {
            var match = NumericMapping.Match(lines[index].TrimEnd('\r'));
            if (!match.Success)
            {
                continue;
            }

            var number = match.Groups["number"];
            if (!LiteralExpressionResolver.TryRead(number.Value, out var value) || value is not (long or decimal or double))
            {
                continue;
            }

            literals[index + 1] = number.Value;
            if (number.Value.Contains('e', StringComparison.OrdinalIgnoreCase))
            {
                lines[index] = $"{lines[index][..number.Index]}0{lines[index][(number.Index + number.Length)..]}";
            }
        }

        return (string.Join('\n', lines), literals);
    }
}
