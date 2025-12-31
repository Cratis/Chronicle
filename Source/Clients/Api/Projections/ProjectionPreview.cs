// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Api.Projections;

/// <summary>
/// Represents a preview of a projection.
/// </summary>
/// <param name="ReadModelEntries">The read model entries resulting from the projection preview.</param>
public record ProjectionPreview(IEnumerable<JsonObject> ReadModelEntries);
