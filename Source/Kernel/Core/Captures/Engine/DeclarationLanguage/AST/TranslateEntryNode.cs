// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.AST;

/// <summary>
/// Represents a single translate entry.
/// </summary>
/// <param name="From">The source value.</param>
/// <param name="To">The target value.</param>
public record TranslateEntryNode(string From, string To) : AstNode;
