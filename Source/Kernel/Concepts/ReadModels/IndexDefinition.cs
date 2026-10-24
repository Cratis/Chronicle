// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Concepts.ReadModels;

/// <summary>
/// Represents the ordered properties of a read-model index.
/// </summary>
/// <param name="PropertyPath">The leading property path, also understood by older kernels.</param>
[GenerateSerializer]
[Alias(nameof(IndexDefinition))]
public record IndexDefinition([property: Id(0)] PropertyPath PropertyPath)
{
    [Id(1)]
    IReadOnlyList<IndexedProperty>? _properties;

    /// <summary>
    /// Gets the ordered properties, falling back to the legacy ascending property path.
    /// </summary>
    public IReadOnlyList<IndexedProperty> Properties
    {
        get => _properties is { Count: > 0 } ? _properties : [new(PropertyPath)];
        init => _properties = value;
    }

    /// <summary>
    /// Gets whether the index contains multiple properties.
    /// </summary>
    public bool IsComposite => Properties.Count > 1;

    /// <summary>
    /// Gets an unambiguous, ordered signature including sort direction.
    /// </summary>
    public string Signature => string.Join('|', Properties.Select(property => $"{property.Path.Path.Length}:{property.Path.Path}:{(property.Descending ? "desc" : "asc")}"));

    /// <inheritdoc/>
    public virtual bool Equals(IndexDefinition? other) => other is not null && EqualityContract == other.EqualityContract && Properties.SequenceEqual(other.Properties);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        HashCode hash = default;
        hash.Add(EqualityContract);
        foreach (var property in Properties)
        {
            hash.Add(property);
        }
        return hash.ToHashCode();
    }
}
