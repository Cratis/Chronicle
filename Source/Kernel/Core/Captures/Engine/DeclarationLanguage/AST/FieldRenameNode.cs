// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.AST;

/// <summary>
/// Represents a field rename operation.
/// </summary>
/// <param name="TargetProperty">The target property.</param>
/// <param name="SourceProperty">The source property.</param>
public record FieldRenameNode(string TargetProperty, string SourceProperty) : MapOperationNode;
