// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.ReadModelTypes;

/// <summary>
/// Represents the command for creating a read model.
/// </summary>
/// <param name="Identifier">Identifier of the read model.</param>
/// <param name="DisplayName">Display name of the read model.</param>
/// <param name="ContainerName">Container name of the read model to create (collection, table, etc.).</param>
/// <param name="Schema">Optional schema for the read model.</param>
public record CreateReadModel(string Identifier, string DisplayName, string ContainerName, string? Schema = null);
