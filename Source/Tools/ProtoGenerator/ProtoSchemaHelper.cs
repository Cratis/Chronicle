// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.RegularExpressions;

namespace Cratis.Chronicle.Tools.ProtoGenerator;

/// <summary>
/// Helper class for fixing proto3 schema compatibility issues.
/// </summary>
internal static partial class ProtoSchemaHelper
{
    /// <summary>
    /// Fixes enum value naming conflicts in a proto3 schema.
    /// In proto3, enum values use C++ scoping rules and must be unique within the package.
    /// When conflicts are detected, the conflicting values are prefixed with
    /// an UPPER_SNAKE_CASE version of their parent enum name.
    /// </summary>
    /// <param name="schema">The proto schema string to process.</param>
    /// <returns>The schema with enum value conflicts resolved.</returns>
    public static string FixEnumValueConflicts(string schema)
    {
        var enumBlockRegex = EnumBlockRegex();
        var valueDeclarationRegex = ValueDeclarationRegex();

        // First pass: collect all value names per enum
        var fullMatches = new List<string>();
        var enumNames = new List<string>();
        var enumBodies = new List<string>();
        var allEnumValues = new List<List<string>>();

        foreach (Match m in enumBlockRegex.Matches(schema))
        {
            fullMatches.Add(m.Value);
            enumNames.Add(m.Groups["name"].Value);
            var body = m.Groups["body"].Value;
            enumBodies.Add(body);
            var values = new List<string>();
            foreach (Match vm in valueDeclarationRegex.Matches(body))
            {
                values.Add(vm.Groups["value"].Value);
            }

            allEnumValues.Add(values);
        }

        // Find value names that appear in more than one enum within this schema
        var valueCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var values in allEnumValues)
        {
            foreach (var value in values)
            {
                valueCounts.TryGetValue(value, out var count);
                valueCounts[value] = count + 1;
            }
        }

        var conflictingNames = valueCounts
            .Where(kv => kv.Value > 1)
            .Select(kv => kv.Key)
            .ToHashSet(StringComparer.Ordinal);

        if (conflictingNames.Count == 0)
        {
            return schema;
        }

        // Second pass: prefix the conflicting values in each affected enum
        var result = schema;
        for (var i = 0; i < enumNames.Count; i++)
        {
            var conflictingValues = allEnumValues[i].Where(conflictingNames.Contains).ToList();
            if (conflictingValues.Count == 0)
            {
                continue;
            }

            var prefix = ToUpperSnakeCase(enumNames[i]) + "_";
            var newBody = PrefixValuesInBody(enumBodies[i], conflictingValues, prefix);
            result = result.Replace(fullMatches[i], fullMatches[i].Replace(enumBodies[i], newBody));
        }

        return result;
    }

    /// <summary>
    /// Converts a PascalCase identifier to UPPER_SNAKE_CASE.
    /// For example: JobStepStatus → JOB_STEP_STATUS.
    /// </summary>
    /// <param name="name">The PascalCase identifier to convert.</param>
    /// <returns>The UPPER_SNAKE_CASE version of the identifier.</returns>
    public static string ToUpperSnakeCase(string name)
    {
        var sb = new StringBuilder(name.Length * 2);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (i > 0
                && char.IsUpper(c)
                && (char.IsLower(name[i - 1]) || (i + 1 < name.Length && char.IsLower(name[i + 1]))))
            {
                sb.Append('_');
            }

            sb.Append(char.ToUpperInvariant(c));
        }

        return sb.ToString();
    }

    [GeneratedRegex(@"enum\s+(?<name>\w+)\s*\{(?<body>[^{}]*)\}", RegexOptions.Singleline | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex EnumBlockRegex();

    [GeneratedRegex(@"^\s+(?<value>[A-Za-z][A-Za-z0-9_]*)\s*=", RegexOptions.Multiline | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex ValueDeclarationRegex();

    private static string PrefixValuesInBody(string body, List<string> valuesToPrefix, string prefix)
    {
        var lines = body.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimStart();
            foreach (var value in valuesToPrefix)
            {
                if (!trimmed.StartsWith(value, StringComparison.Ordinal))
                {
                    continue;
                }

                var afterValue = trimmed[value.Length..];
                var isValueDeclaration = afterValue.StartsWith('=')
                    || afterValue.StartsWith(" =", StringComparison.Ordinal)
                    || afterValue.StartsWith("\t=", StringComparison.Ordinal);
                if (!isValueDeclaration)
                {
                    continue;
                }

                var indent = lines[i].Length - trimmed.Length;
                lines[i] = string.Concat(lines[i].AsSpan(0, indent), prefix, trimmed);
                break;
            }
        }

        return string.Join('\n', lines);
    }
}
