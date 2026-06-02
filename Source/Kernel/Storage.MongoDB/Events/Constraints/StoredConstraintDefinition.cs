// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints;

/// <summary>
/// Represents a stored constraint definition entry.
/// </summary>
/// <param name="Id">The persisted unique identifier.</param>
/// <param name="Name">The constraint name.</param>
/// <param name="Version">The definition version.</param>
/// <param name="Definition">The serialized definition.</param>
public record StoredConstraintDefinition(string Id, string Name, ulong Version, IConstraintDefinition Definition);
