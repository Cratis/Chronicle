// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.DefinitionLanguage.AST;

/// <summary>
/// Represents a literal string part in a template.
/// </summary>
/// <param name="Text">The literal text.</param>
public record TemplateTextPart(string Text) : TemplatePart;
