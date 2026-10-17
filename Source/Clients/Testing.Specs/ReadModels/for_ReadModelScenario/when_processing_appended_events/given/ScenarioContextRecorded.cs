// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_processing_appended_events.given;

/// <summary>
/// Records a value for the appended-context scenario specifications.
/// </summary>
/// <param name="Value">The recorded value.</param>
[EventType]
public record ScenarioContextRecorded(string Value);
