// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.given;

/// <summary>
/// Projects a name onto a read model whose JSON payload only ever comes from the initial state.
/// </summary>
/// <param name="Id">The read model identifier.</param>
/// <param name="Name">The name set by <see cref="SeededJsonRenamed"/>.</param>
/// <param name="Payload">The JSON payload carried by the initial state.</param>
[Passive]
[FromEvent<SeededJsonRenamed>]
public record SeededJsonReadModel(Guid Id, string Name, JsonObject Payload);
