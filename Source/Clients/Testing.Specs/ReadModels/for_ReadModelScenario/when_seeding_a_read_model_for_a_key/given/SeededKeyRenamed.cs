// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_key.given;

/// <summary>
/// Represents a change to the caption of a seeded instance.
/// </summary>
/// <param name="Caption">The new caption.</param>
[EventType]
public record SeededKeyRenamed(string Caption);
