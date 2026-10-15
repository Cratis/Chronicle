// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_routed_events.given;

/// <summary>
/// Records input used to verify routed scenario history.
/// </summary>
/// <param name="Value">The recorded value.</param>
[EventType]
public record RoutedEventRecorded(string Value);
