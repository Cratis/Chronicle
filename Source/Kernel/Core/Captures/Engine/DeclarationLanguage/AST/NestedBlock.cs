// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.AST;

/// <summary>
/// Represents a nested object block.
/// </summary>
/// <param name="ObjectPath">The nested object path.</param>
/// <param name="Map">The optional map block.</param>
/// <param name="Appends">The append blocks.</param>
public record NestedBlock(string ObjectPath, MapBlock? Map, IReadOnlyList<AppendBlock> Appends) : CaptureDirective;
