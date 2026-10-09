// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.Engine;

/// <summary>
/// Represents a private event a capture decided to append.
/// </summary>
/// <param name="Append">The <see cref="AppendDefinition"/> that matched.</param>
/// <param name="Content">The content of the event to append.</param>
public record TranslatedCaptureEvent(AppendDefinition Append, JsonObject Content);
