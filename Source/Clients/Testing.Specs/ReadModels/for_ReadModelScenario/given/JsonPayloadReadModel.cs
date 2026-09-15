// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.given;

/// <summary>
/// Projects an event's JSON payload without interpreting its keys.
/// </summary>
/// <param name="Id">The read model identifier.</param>
/// <param name="Payload">The projected JSON payload.</param>
[Passive]
[FromEvent<JsonPayloadRecorded>]
public record JsonPayloadReadModel(Guid Id, JsonObject Payload);
