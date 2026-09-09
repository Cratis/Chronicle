// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.given;

/// <summary>
/// Records an opaque JSON payload to exercise event input serialization.
/// </summary>
/// <param name="Payload">The JSON payload.</param>
[EventType]
public record JsonPayloadRecorded(JsonObject Payload);
