// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.AST;

/// <summary>
/// Represents a translate operation.
/// </summary>
/// <param name="Target">The target property.</param>
/// <param name="Source">The source property.</param>
/// <param name="Entries">The translate entries.</param>
public record TranslateNode(string Target, string Source, IReadOnlyList<TranslateEntryNode> Entries) : MapOperationNode;
