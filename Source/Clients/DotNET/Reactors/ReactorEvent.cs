// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Represents an event delivered to a runtime-registered reactor without CLR event deserialization.
/// </summary>
/// <param name="Context">The context delivered by the kernel, including its event type generation.</param>
/// <param name="Content">The JSON content delivered by the kernel.</param>
/// <param name="GenerationalContent">The kernel's generation-number-to-JSON-content map, without selecting or converting a generation.</param>
public record ReactorEvent(EventContext Context, JsonObject Content, IReadOnlyDictionary<int, string> GenerationalContent);
