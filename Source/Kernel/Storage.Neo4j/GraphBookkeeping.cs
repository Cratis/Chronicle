// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Neo4j;

/// <summary>
/// Holds the names of the properties and the label Chronicle keeps in the graph for itself.
/// </summary>
/// <remarks>
/// Every name starts with <see cref="Prefix"/>. A property of the graph definition must not start with it, which
/// <see cref="ReservedGraphPropertyName"/> enforces, so the bookkeeping can never collide with a model property.
/// </remarks>
public static class GraphBookkeeping
{
    /// <summary>
    /// The prefix of everything Chronicle keeps for itself.
    /// </summary>
    public const string Prefix = "_";

    /// <summary>
    /// The sequence number of the last event that wrote the element. Absent on a node that only an edge created.
    /// </summary>
    public const string Watermark = Prefix + "watermark";

    /// <summary>
    /// The projection that owns the element.
    /// </summary>
    public const string Owner = Prefix + "owner";

    /// <summary>
    /// The replay generation of the owner the element was last written in.
    /// </summary>
    public const string Generation = Prefix + "generation";

    /// <summary>
    /// The namespace of the element, stamped on everything in single database mode.
    /// </summary>
    public const string Namespace = Prefix + "namespace";

    /// <summary>
    /// The label of the node that keeps the current replay generation of an owner.
    /// </summary>
    public const string OwnerLabel = Prefix + "Owner";

    /// <summary>
    /// The reserved label of database-local sink provenance metadata.
    /// </summary>
    public const string ProvenanceLabel = "__cratis_sink_inventory";

    /// <summary>
    /// Ensure a property name does not collide with the bookkeeping.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <exception cref="ReservedGraphPropertyName">Thrown when the name starts with <see cref="Prefix"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when the name contains a backslash.</exception>
    public static void EnsureNotReserved(string name)
    {
        GraphCypher.ValidateIdentifier(name);
        if (name.StartsWith('_'))
        {
            throw new ReservedGraphPropertyName(name);
        }
    }

    /// <summary>
    /// Validates that an application label cannot address provenance metadata.
    /// </summary>
    /// <param name="label">The application label.</param>
    /// <exception cref="ArgumentException">Thrown when the label contains a backslash.</exception>
    /// <exception cref="ReservedGraphPropertyName">Thrown when the label is reserved for provenance.</exception>
    internal static void EnsureApplicationLabel(string label)
    {
        GraphCypher.ValidateIdentifier(label);
        if (label == ProvenanceLabel) throw new ReservedGraphPropertyName(label);
    }
}
