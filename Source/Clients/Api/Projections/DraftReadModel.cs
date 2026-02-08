// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Projections;

/// <summary>
/// Represents a draft read model definition that can be used for preview/save operations
/// before the read model type is actually created.
/// </summary>
/// <param name="ContainerName">Container name of the read model (collection, table, etc.).</param>
/// <param name="Schema">JSON schema for the read model.</param>
public record DraftReadModel(string ContainerName, string Schema);
