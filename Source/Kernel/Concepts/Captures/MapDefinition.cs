// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Captures;

/// <summary>
/// Represents the mapping configuration for a capture scope.
/// </summary>
/// <param name="Operations">The operations in the map block.</param>
public record MapDefinition(IReadOnlyList<MapOperation> Operations);
