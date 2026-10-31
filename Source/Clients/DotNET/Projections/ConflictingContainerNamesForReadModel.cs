// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// The exception that is thrown when projections of the same read model declare different container names.
/// </summary>
/// <param name="readModelType">The read model type the projections maintain.</param>
/// <param name="existingContainerName">The container name already declared for the read model.</param>
/// <param name="conflictingContainerName">The container name that conflicts with it.</param>
public class ConflictingContainerNamesForReadModel(Type readModelType, string existingContainerName, string conflictingContainerName)
    : Exception($"Read model '{readModelType.FullName}' is given container name '{conflictingContainerName}' by one projection and '{existingContainerName}' by another. A read model has a single container, so every projection of it must use the same container name.");
