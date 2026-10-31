// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.given;

/// <summary>
/// Captures the occurrence time delivered to a reducer.
/// </summary>
/// <param name="Occurred">The occurrence time from the event context.</param>
public record RecordedOccurrence(DateTimeOffset Occurred);
