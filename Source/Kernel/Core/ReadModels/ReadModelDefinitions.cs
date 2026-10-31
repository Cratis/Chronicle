// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Compares persisted definition content rather than collection and schema object identities.
/// </summary>
internal static class ReadModelDefinitions
{
    /// <summary>
    /// Determines whether every persisted property has identical content.
    /// </summary>
    /// <param name="first">The existing definition, if any.</param>
    /// <param name="second">The incoming definition.</param>
    /// <returns>Whether no persisted property changed.</returns>
    internal static bool AreEqual(ReadModelDefinition? first, ReadModelDefinition second) =>
        first is not null &&
        first.Identifier == second.Identifier &&
        first.ContainerName == second.ContainerName &&
        first.DisplayName == second.DisplayName &&
        first.Owner == second.Owner &&
        first.Source == second.Source &&
        first.ObserverType == second.ObserverType &&
        first.ObserverIdentifier == second.ObserverIdentifier &&
        first.Sink == second.Sink &&
        first.Indexes.SequenceEqual(second.Indexes) &&
        first.Schemas.Count == second.Schemas.Count &&
        first.Schemas.All(pair => second.Schemas.TryGetValue(pair.Key, out var schema) && pair.Value.ToJson() == schema.ToJson());
}
