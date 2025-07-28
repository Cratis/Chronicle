// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.ReadModels;

/// <summary>
/// Exception thrown when a read model definition does not have a schema defined for the latest generation.
/// </summary>
/// <param name="name">The name of the read model.</param>
public class MissingSchemaForReadModel(ReadModelName name) : Exception($"The read model definition for '{name.Value}' does not have a schema defined for the latest generation.");
