// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.CSharp;

/// <summary>
/// Resolves references to the record name selected for a schema during one generation.
/// </summary>
sealed class RecordNames
{
    readonly Dictionary<string, string> _names = new(StringComparer.Ordinal);

    /// <summary>
    /// Resolves a schema name, reusing the first name for references to an untitled object.
    /// </summary>
    /// <param name="schema">The original property or item schema, before resolving references.</param>
    /// <param name="fallback">The name derived from the enclosing record.</param>
    /// <returns>The name used to declare and reference the record.</returns>
    internal string Resolve(JsonSchema schema, string fallback)
    {
        var actual = schema.ActualSchema;
        var name = CSharpDefinitionSupport.RecordName(actual, fallback);
        if (!actual.Type.HasFlag(JsonObjectType.Object)) return name;
        var identity = actual.ToJson();
        if (schema.HasReference && _names.TryGetValue(identity, out var referencedName)) return referencedName;
        _names.TryAdd(identity, name);
        return name;
    }
}
