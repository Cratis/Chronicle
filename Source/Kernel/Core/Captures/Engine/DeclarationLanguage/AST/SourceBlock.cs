// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.AST;

/// <summary>
/// Represents a source block.
/// </summary>
/// <param name="SourceType">The source type.</param>
/// <param name="Properties">The source properties.</param>
public record SourceBlock(SourceType SourceType, IReadOnlyDictionary<string, string> Properties) : CaptureDirective;
