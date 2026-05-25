// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.AST;

/// <summary>
/// Represents a split operation.
/// </summary>
/// <param name="Source">The source property.</param>
/// <param name="Separator">The separator to split by.</param>
/// <param name="Targets">The target properties.</param>
public record SplitNode(string Source, string Separator, IReadOnlyList<string> Targets) : MapOperationNode;
