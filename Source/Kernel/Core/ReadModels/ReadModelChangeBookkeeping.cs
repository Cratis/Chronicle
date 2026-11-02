// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Separates kernel bookkeeping from the document of a read model change, so that compliance only ever sees the
/// properties the read model's schema declares (Cratis/Chronicle#3588).
/// </summary>
internal static class ReadModelChangeBookkeeping
{
    /// <summary>
    /// Reads the subject lineage of a read model state.
    /// </summary>
    /// <param name="state">The state carrying the lineage.</param>
    /// <returns>The default subject and the subject of each property that differs from it; <see langword="null"/> when absent.</returns>
    public static (string? Subject, IReadOnlyDictionary<string, string>? Subjects) LineageOf(ExpandoObject state)
    {
        var dictionary = (IDictionary<string, object?>)state;
        var subject = dictionary.TryGetValue(WellKnownProperties.Subject, out var subjectValue) && subjectValue is string text
            ? text
            : null;
        var subjects = dictionary.TryGetValue(WellKnownProperties.Subjects, out var subjectsValue)
            ? ReadModelSubjects.From(subjectsValue)
            : new Dictionary<string, string>();
        return (subject, subjects.Count > 0 ? subjects : null);
    }

    /// <summary>
    /// Splits a change document into the payload compliance may see and the subject lineage that travels with it.
    /// </summary>
    /// <param name="document">The change document. It is not modified.</param>
    /// <param name="change">The <see cref="ReadModelChangeContext"/> accompanying the document.</param>
    /// <param name="schema">The <see cref="JsonSchema"/> of the read model.</param>
    /// <returns>The payload and the subject lineage. The lineage comes from the change, then from the document of a producer of an earlier release.</returns>
    public static (JsonObject Payload, string? Subject, IReadOnlyDictionary<string, string> Subjects) Split(JsonObject document, ReadModelChangeContext? change, JsonSchema schema)
    {
        var subject = change?.Subject ?? document[WellKnownProperties.Subject]?.GetValue<string>();
        var subjects = change?.Subjects ?? ReadModelSubjects.From(document[WellKnownProperties.Subjects]);
        return (WithoutUndeclaredBookkeeping(document, schema), subject, subjects);
    }

    /// <summary>
    /// Copies subject lineage for consumers of the previous release.
    /// </summary>
    /// <param name="state">The state carrying lineage.</param>
    /// <param name="document">The outgoing document.</param>
    public static void CopyLegacySubjectLineage(ExpandoObject state, JsonObject document)
    {
        // Remove in the next major: silos of the previous release read the subject from the document (Cratis/Chronicle#3588).
        ReadModelSubjects.CopyToJson(state, document);
    }

    /// <summary>
    /// Checks whether the schema declares a property.
    /// </summary>
    /// <param name="schema">The read model schema.</param>
    /// <param name="property">The property name.</param>
    /// <returns>Whether the property is declared.</returns>
    public static bool DeclaresProperty(JsonSchema schema, string property) =>
        schema.GetFlattenedProperties().Any(_ => _.Name == property);

    /// <summary>
    /// Copies a document without the kernel bookkeeping properties its schema does not declare. A read model is free
    /// to expose a bookkeeping property as its own, in which case it is a property like any other and survives.
    /// </summary>
    /// <param name="document">The document. It is not modified.</param>
    /// <param name="schema">The <see cref="JsonSchema"/> of the read model.</param>
    /// <returns>The copy.</returns>
    public static JsonObject WithoutUndeclaredBookkeeping(JsonObject document, JsonSchema schema)
    {
        var declared = schema.GetFlattenedProperties().Select(_ => _.Name).ToHashSet(StringComparer.Ordinal);
        var copy = (document.DeepClone() as JsonObject)!;
        foreach (var property in WellKnownProperties.All.Where(_ => !declared.Contains(_)))
        {
            copy.Remove(property);
        }

        return copy;
    }
}
