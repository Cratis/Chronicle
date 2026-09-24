// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.given;

/// <summary>
/// Renames a <see cref="SeededJsonReadModel"/> without touching its payload.
/// </summary>
/// <param name="Name">The new name.</param>
[EventType]
public record SeededJsonRenamed(string Name);
