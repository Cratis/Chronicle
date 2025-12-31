// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.DSL;

/// <summary>
/// Abstraction for parsing projection DSL strings into <see cref="ProjectionDefinition"/> instances.
/// </summary>
public interface IProjectionDslParser
{
    /// <summary>
    /// Parse the provided DSL into a <see cref="ProjectionDefinition"/>.
    /// </summary>
    ProjectionDefinition Parse(string dsl, ProjectionId identifier, ProjectionOwner owner, EventSequenceId eventSequenceId);
}
