// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;

namespace Cratis.Chronicle.Api.ReadModelTypes;

/// <summary>
/// Represents a read model definition.
/// </summary>
/// <param name="Identifier">Identifier of the read model.</param>
/// <param name="Name">Name of the read model.</param>
/// <param name="Generation">Generation of the read model.</param>
/// <param name="Schema">JSON schema for the read model.</param>
/// <param name="Indexes">Collection of property paths for indexes.</param>
public record ReadModelDefinition(string Identifier, string Name, ulong Generation, string Schema, IEnumerable<string> Indexes);
