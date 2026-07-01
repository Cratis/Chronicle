// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.AST;

/// <summary>
/// Represents a field assignment inside an append block.
/// </summary>
/// <param name="Target">The target field.</param>
/// <param name="Source">The source expression.</param>
public record FieldAssignmentNode(string Target, string Source) : AstNode;
