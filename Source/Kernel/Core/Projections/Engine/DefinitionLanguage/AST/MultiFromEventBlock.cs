// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage.AST;

/// <summary>
/// Represents multiple from event blocks that share the same body (parsed from comma-separated events).
/// </summary>
/// <param name="Blocks">The individual from event blocks.</param>
public record MultiFromEventBlock(IReadOnlyList<FromEventBlock> Blocks) : ProjectionDirective;
