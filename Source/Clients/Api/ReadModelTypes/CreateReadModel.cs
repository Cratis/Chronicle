// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.ReadModelTypes;

/// <summary>
/// Represents the command for creating a read model.
/// </summary>
/// <param name="Name">Name of the read model to create.</param>
/// <param name="Schema">Optional schema for the read model.</param>
public record CreateReadModel(string Name, string? Schema = null);
