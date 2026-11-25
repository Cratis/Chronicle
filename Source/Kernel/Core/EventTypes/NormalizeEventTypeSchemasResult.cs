// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Represents the generations actually normalized by an explicit cleanup request.
/// </summary>
/// <param name="Changed">The changed generations and removed keywords.</param>
public record NormalizeEventTypeSchemasResult(IEnumerable<EventTypesWithAnnotations> Changed);
