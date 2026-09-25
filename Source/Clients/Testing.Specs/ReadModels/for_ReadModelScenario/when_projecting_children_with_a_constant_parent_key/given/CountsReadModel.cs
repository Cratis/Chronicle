// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_children_with_a_constant_parent_key.given;

/// <summary>
/// A fixed-key document that counts across every event source - a root total alongside a membership set.
/// </summary>
/// <param name="Id">The document's identity, which is the constant the projection keys on.</param>
/// <param name="Registrations">The root's own running total.</param>
/// <param name="Things">The membership set.</param>
public record CountsReadModel(string Id, int Registrations, IEnumerable<CountedThing> Things)
{
    /// <summary>
    /// Gets how many things are still open.
    /// </summary>
    public int Open => Things?.Count(thing => thing.IsOpen) ?? 0;
}
