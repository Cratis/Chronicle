// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_routed_events.given;

/// <summary>
/// Counts events delivered through an explicit default source filter.
/// </summary>
/// <param name="Count">The number of delivered events.</param>
public record DefaultSourceEventRoute(int Count);
