// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.given;

/// <summary>
/// Reducer-backed read model carrying a JSON payload that only ever comes from the initial state.
/// </summary>
/// <param name="Id">The read model identifier.</param>
/// <param name="Count">Running count, incremented once per <see cref="SeededJsonTallied"/> event.</param>
/// <param name="Payload">The JSON payload carried by the initial state.</param>
public record SeededJsonTally(Guid Id, int Count, JsonObject Payload);
