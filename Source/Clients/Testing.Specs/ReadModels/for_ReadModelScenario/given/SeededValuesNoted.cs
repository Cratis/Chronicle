// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.given;

/// <summary>
/// Sets the note of a <see cref="SeededValuesReadModel"/> without touching its seeded values.
/// </summary>
/// <param name="Note">The note.</param>
[EventType]
public record SeededValuesNoted(string Note);
