// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Updates a child by identity on its own event source.
/// </summary>
/// <param name="ParentId">The immediate parent.</param>
/// <param name="ChildId">The child identity.</param>
/// <param name="NewName">The new name.</param>
[EventType]
public record ReplayShapeChildRenamed(string ParentId, string ChildId, string NewName);
