// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

/// <summary>
/// A materialized read model used to specify required seeding in reactor scenarios.
/// </summary>
/// <param name="Id">The model's key.</param>
/// <param name="Host">The host to record.</param>
[FromEvent<VibeStarted>]
public record ScenarioReadModel([Key] string Id, string Host);
