// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.AST;

/// <summary>
/// Represents a map block.
/// </summary>
/// <param name="Operations">The map operations.</param>
public record MapBlock(IReadOnlyList<MapOperationNode> Operations) : CaptureDirective;
