// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage.AST;

/// <summary>
/// Represents a join block.
/// </summary>
/// <param name="JoinName">The name of the join (collection/read model being joined).</param>
/// <param name="OnProperty">The property to join on.</param>
/// <param name="WithBlocks">Collection of 'with' event blocks that populate the join.</param>
public record JoinBlock(
    string JoinName,
    string OnProperty,
    IReadOnlyList<WithEventBlock> WithBlocks) : ProjectionDirective;
