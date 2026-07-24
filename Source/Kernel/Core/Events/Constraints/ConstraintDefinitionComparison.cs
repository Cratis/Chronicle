// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Provides a content-derived version stamp for a set of registered constraint definitions and derives the reindex work
/// required when they change.
/// </summary>
public static class ConstraintDefinitionComparison
{
    static readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = false
    };

    /// <summary>
    /// Compute a <see cref="ConstraintsVersion"/> for a set of constraint definitions.
    /// </summary>
    /// <param name="definitions">The definitions to compute the version for.</param>
    /// <returns>A <see cref="ConstraintsVersion"/> derived from the content of the definitions.</returns>
    /// <remarks>
    /// The version is a hash over the definitions serialized to a canonical, name-ordered form. It therefore depends only
    /// on the content of the definitions — two equal sets produce the same version even when they are distinct instances
    /// read from persisted state on different silos or after grain reactivation, and any add or change produces a
    /// different version.
    /// </remarks>
    public static ConstraintsVersion ComputeVersion(IEnumerable<IConstraintDefinition> definitions)
    {
        var builder = new StringBuilder();
        foreach (var definition in definitions.OrderBy(_ => _.Name.Value, StringComparer.Ordinal))
        {
            builder
                .Append(definition.GetType().Name)
                .Append(':')
                .Append(JsonSerializer.Serialize(definition, definition.GetType(), _serializerOptions))
                .Append('\n');
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Derive the changes for the unique constraints that were added or changed and therefore need their index rebuilt.
    /// </summary>
    /// <param name="previous">The previously observed definitions.</param>
    /// <param name="current">The current definitions.</param>
    /// <returns>The <see cref="ConstraintDefinitionChange"/> for every unique constraint requiring a reindex.</returns>
    public static IReadOnlyCollection<ConstraintDefinitionChange> GetReindexChanges(
        IReadOnlyCollection<IConstraintDefinition> previous,
        IReadOnlyCollection<IConstraintDefinition> current)
    {
        var previousUniqueByName = previous.OfType<UniqueConstraintDefinition>().ToDictionary(_ => _.Name);
        var changes = new List<ConstraintDefinitionChange>();
        foreach (var unique in current.OfType<UniqueConstraintDefinition>())
        {
            if (!previousUniqueByName.TryGetValue(unique.Name, out var existing))
            {
                changes.Add(new ConstraintDefinitionChange(unique.Name, true, [ConstraintChangeType.EventAdded, ConstraintChangeType.IndexedPropertiesChanged]));
                continue;
            }

            var change = unique.CompareWith(existing);
            if (change.RequiresReindex)
            {
                changes.Add(new ConstraintDefinitionChange(unique.Name, true, change.ChangeTypes));
            }
        }

        return changes;
    }
}
