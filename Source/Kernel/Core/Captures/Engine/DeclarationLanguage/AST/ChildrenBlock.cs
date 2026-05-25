// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.AST;

/// <summary>
/// Represents a children block.
/// </summary>
/// <param name="CollectionProperty">The collection property.</param>
/// <param name="IdentifiedBy">The child identifier property.</param>
/// <param name="Map">The optional map block.</param>
/// <param name="Appends">The append blocks.</param>
public record ChildrenBlock(
    string CollectionProperty,
    string IdentifiedBy,
    MapBlock? Map,
    IReadOnlyList<AppendBlock> Appends) : CaptureDirective;
