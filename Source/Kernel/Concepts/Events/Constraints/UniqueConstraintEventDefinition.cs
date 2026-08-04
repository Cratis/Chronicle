// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints;

/// <summary>
/// Represents a definition of a unique event type and property constraint.
/// </summary>
/// <param name="EventTypeId">The <see cref="EventTypeId"/>.</param>
/// <param name="Properties">The properties on the event type.</param>
public record UniqueConstraintEventDefinition(EventTypeId EventTypeId, IEnumerable<string> Properties)
{
    /// <summary>
    /// Compare with another <see cref="UniqueConstraintEventDefinition"/> by value.
    /// </summary>
    /// <param name="other">The definition to compare with.</param>
    /// <returns>True if the definitions carry the same content, false otherwise.</returns>
    /// <remarks>
    /// The generated record equality would compare the properties by reference, and the definition this belongs
    /// to is rebuilt from the client's attributes on every connect - so a fresh sequence carrying identical
    /// names would never equal the stored one, and registration would report an unchanged constraint as changed
    /// on every startup.
    /// </remarks>
    public virtual bool Equals(UniqueConstraintEventDefinition? other) =>
        other is not null &&
        EventTypeId == other.EventTypeId &&
        Properties.SequenceEqual(other.Properties);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hashCode = default(HashCode);
        hashCode.Add(EventTypeId);
        foreach (var property in Properties)
        {
            hashCode.Add(property);
        }

        return hashCode.ToHashCode();
    }
}
