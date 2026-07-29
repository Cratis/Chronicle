// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Captures;

/// <summary>
/// Represents a nested object scope in a capture.
/// </summary>
/// <param name="ObjectPath">The object path.</param>
/// <param name="Map">Optional map definition.</param>
/// <param name="Appends">Append definitions for the nested scope.</param>
public record NestedDefinition(
    string ObjectPath,
    MapDefinition? Map,
    IReadOnlyList<AppendDefinition> Appends);
