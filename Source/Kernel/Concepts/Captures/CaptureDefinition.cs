// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Captures;

/// <summary>
/// Represents the definition of a capture.
/// </summary>
/// <param name="Id">The <see cref="CaptureId"/>.</param>
/// <param name="Source">The source definition.</param>
/// <param name="KeyProperty">The identity property used for diffing.</param>
/// <param name="Map">Optional root map definition.</param>
/// <param name="Appends">Append definitions on the root scope.</param>
/// <param name="Nested">Nested object scopes.</param>
/// <param name="Children">Child collection scopes.</param>
public record CaptureDefinition(
    CaptureId Id,
    SourceDefinition Source,
    string KeyProperty,
    MapDefinition? Map,
    IReadOnlyList<AppendDefinition> Appends,
    IReadOnlyList<NestedDefinition> Nested,
    IReadOnlyList<ChildrenDefinition> Children);
