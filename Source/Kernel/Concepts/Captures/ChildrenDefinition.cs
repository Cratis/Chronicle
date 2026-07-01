// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Captures;

/// <summary>
/// Represents a child collection scope in a capture.
/// </summary>
/// <param name="CollectionProperty">The collection property.</param>
/// <param name="IdentifiedBy">The child identity property.</param>
/// <param name="Map">Optional map definition.</param>
/// <param name="Appends">Append definitions for the child scope.</param>
public record ChildrenDefinition(
    string CollectionProperty,
    string IdentifiedBy,
    MapDefinition? Map,
    IReadOnlyList<AppendDefinition> Appends);
