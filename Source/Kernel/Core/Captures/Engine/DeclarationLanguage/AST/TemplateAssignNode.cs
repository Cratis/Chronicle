// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.AST;

/// <summary>
/// Represents a template assignment operation.
/// </summary>
/// <param name="Target">The target property.</param>
/// <param name="Template">The template.</param>
public record TemplateAssignNode(string Target, string Template) : MapOperationNode;
