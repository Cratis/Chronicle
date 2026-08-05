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
    /// The name of the attribute that marks retired proto field numbers.
    /// </summary>
    /// <remarks>
    /// Matched by name because this generator does not reference the contracts assembly - it loads it. Renaming the
    /// attribute without changing this would stop reserving field numbers, so a specification pins the two together
    /// against the real attribute type.
    /// </remarks>
    internal const string ReservedProtoFieldsAttributeName = "ReservedProtoFieldsAttribute";

    /// <summary>
    /// The name of the property on that attribute carrying the retired field numbers.
    /// </summary>
    internal const string FieldNumbersPropertyName = "FieldNumbers";

    [GeneratedRegex(@"^package\s+(?<name>[\w.]+)\s*;", RegexOptions.Multiline | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex PackageDeclarationRegex { get; }

    [GeneratedRegex(@"^message\s+(?<name>\w+)\s*\{", RegexOptions.Multiline | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex MessageDeclarationRegex { get; }

    [GeneratedRegex(@"rpc\s+(?<method>\w+)\s*\(\s*(?<input>\w+)\s*\)", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex RpcDeclarationRegex { get; }

    [GeneratedRegex(@"enum\s+(?<name>\w+)\s*\{(?<body>[^{}]*)\}", RegexOptions.Singleline | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex EnumBlockRegex { get; }

    [GeneratedRegex(@"^\s+(?<value>[A-Za-z][A-Za-z0-9_]*)\s*=", RegexOptions.Multiline | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex ValueDeclarationRegex { get; }

    /// <summary>
    /// Fixes RPC method name conflicts in a proto3 schema.
    /// In proto3, any message type that shares a name with an RPC method in the same service
    /// becomes unresolvable — protoc resolves the identifier as the method, not the type.
    /// The fix collects all such conflicting names and globally replaces every unqualified
    /// <c>(TypeName)</c> and <c>(stream TypeName)</c> reference with the fully-qualified
    /// package-prefixed form (e.g., <c>(stream ConnectionKeepAlive)</c> →
    /// <c>(stream .Cratis.Chronicle.Contracts.Clients.ConnectionKeepAlive)</c>).
    /// </summary>
    /// <param name="schema">The proto schema string to process.</param>
    /// <returns>The schema with RPC method name conflicts resolved.</returns>
    public static string FixRpcMethodNameConflicts(string schema)
    {
        var packageMatch = PackageDeclarationRegex.Match(schema);
        if (!packageMatch.Success)
        {
            return schema;
        }

        var packageName = packageMatch.Groups["name"].Value;

        var messageNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match m in MessageDeclarationRegex.Matches(schema))
        {
            messageNames.Add(m.Groups["name"].Value);
        }

        if (messageNames.Count == 0)
        {
            return schema;
        }

        // Collect all RPC method names in this file
        var rpcMethodNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match m in RpcDeclarationRegex.Matches(schema))
        {
            rpcMethodNames.Add(m.Groups["method"].Value);
        }

        // Conflicting types: message types whose name is also used as an RPC method name
        var conflictingTypes = messageNames
            .Where(rpcMethodNames.Contains)
            .ToHashSet(StringComparer.Ordinal);

        if (conflictingTypes.Count == 0)
        {
            return schema;
        }

        // Replace all unqualified type references (in RPC parameter lists) for conflicting types.
        // (TypeName) appears as input type; (stream TypeName) appears as streaming return type.
        var result = schema;
        foreach (var typeName in conflictingTypes)
        {
            var qualified = $".{packageName}.{typeName}";
            result = result
                .Replace($"(stream {typeName})", $"(stream {qualified})")
                .Replace($"({typeName})", $"({qualified})");
        }

        return result;
    }

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
        var enumBlockRegex = EnumBlockRegex;
        var valueDeclarationRegex = ValueDeclarationRegex;

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

    /// <summary>
    /// Emits a <c>reserved</c> declaration for every field number a contract type has retired.
    /// </summary>
    /// <param name="schema">The generated schema.</param>
    /// <param name="types">The contract types the schema was generated from.</param>
    /// <returns>The schema with reservations declared.</returns>
    /// <remarks>
    /// The schema generator has no notion of a retired field, so a <c>reserved</c> line added to the generated
    /// file by hand disappears the next time anyone regenerates - silently, and with nothing to notice it by.
    /// Reading it from the contract instead makes the generated file reproducible, which is the only form a
    /// reservation can survive in.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a type asks for reserved field numbers but none can be emitted for it - the attribute carries no
    /// numbers, its numbers cannot be read, or no matching message exists in the generated schema. A reservation that
    /// quietly does not happen frees the number for reuse again, which is what this exists to prevent.
    /// </exception>
    public static string DeclareReservedFields(string schema, IEnumerable<Type> types)
    {
        foreach (var type in types)
        {
            var reserved = Array.Find(
                type.GetCustomAttributes(inherit: false),
                _ => string.Equals(_.GetType().Name, ReservedProtoFieldsAttributeName, StringComparison.Ordinal));
            if (reserved is null)
            {
                continue;
            }

            // Everything below this point is reached only because a type asked for reserved field numbers, so every
            // way of not producing them is an error rather than a skip. A field number that quietly stops being
            // reserved is available for reuse again, which is the corruption this whole attribute exists to prevent -
            // and it would not show up until a new field silently collided with an old one on the wire.
            var fieldNumbers = reserved.GetType().GetProperty(FieldNumbersPropertyName)?.GetValue(reserved)
                ?? throw new InvalidOperationException(
                    $"'{type.FullName}' carries {ReservedProtoFieldsAttributeName} but it has no readable '{FieldNumbersPropertyName}' property. The attribute and this generator have to agree on that name.");

            var numbers = ((IEnumerable<int>)fieldNumbers).Order().ToArray();
            if (numbers.Length == 0)
            {
                throw new InvalidOperationException(
                    $"'{type.FullName}' carries {ReservedProtoFieldsAttributeName} without any field numbers. Reserve the numbers that were retired, or drop the attribute.");
            }

            var declaration = MessageDeclarationRegex.Matches(schema).FirstOrDefault(_ => _.Groups["name"].Value == type.Name)
                ?? throw new InvalidOperationException(
                    $"'{type.FullName}' carries {ReservedProtoFieldsAttributeName}, but no 'message {type.Name}' was found in the generated schema, so its retired field numbers would not be reserved.");

            var insertAt = declaration.Index + declaration.Length;
            schema = schema[..insertAt] + $"{Environment.NewLine}   reserved {string.Join(", ", numbers)};" + schema[insertAt..];
        }

        return schema;
    }

    /// <summary>
    /// Adds an ISO 8601 format comment above each <c>message SerializableDateTimeOffset</c> block
    /// in the proto schema so that consumers know the expected wire format.
    /// </summary>
    /// <param name="schema">The proto schema string to process.</param>
    /// <returns>The schema with comments added to <c>SerializableDateTimeOffset</c> message definitions.</returns>
    public static string AddSerializableDateTimeOffsetComment(string schema)
    {
        const string messageDeclaration = "message SerializableDateTimeOffset {";
        const string comment = "// Represents a DateTimeOffset value as an ISO 8601 string (e.g., \"2024-01-15T12:30:00.0000000+02:00\").\n";

        return schema.Replace(messageDeclaration, comment + messageDeclaration);
    }

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
