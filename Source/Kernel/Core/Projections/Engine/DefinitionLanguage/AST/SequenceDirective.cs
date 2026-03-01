// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage.AST;

/// <summary>
/// Represents a sequence directive that specifies which event sequence to use.
/// </summary>
/// <param name="SequenceId">The event sequence identifier.</param>
public record SequenceDirective(string SequenceId) : ProjectionDirective;
