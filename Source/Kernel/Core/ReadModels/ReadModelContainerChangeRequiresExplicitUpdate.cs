// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// The exception that is thrown when ordinary registration would redirect an existing read model to another container.
/// </summary>
/// <param name="identifier">The read model identifier.</param>
/// <param name="stored">The stored container name.</param>
/// <param name="requested">The requested container name.</param>
public class ReadModelContainerChangeRequiresExplicitUpdate(ReadModelIdentifier identifier, ReadModelContainerName stored, ReadModelContainerName requested)
    : Exception($"Read model '{identifier}' is registered with container '{stored}', but registration requested '{requested}'. Ordinary registration cannot change its container. Restore the client's naming policy or container mapping to '{stored}', or deliberately change it with UpdateDefinition and coordinate all clients. Then deploy matching clients and arrange any required rebuild/replay separately; UpdateDefinition does not move or rebuild existing data.");
