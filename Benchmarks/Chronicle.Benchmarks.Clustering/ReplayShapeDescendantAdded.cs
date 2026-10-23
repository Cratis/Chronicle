// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Adds a nested child on its own source, resolving its root through its parent.
/// </summary>
/// <param name="ParentId">The immediate parent child.</param>
/// <param name="ChildId">The nested child's identity.</param>
/// <param name="Name">The initial name.</param>
[EventType]
public record ReplayShapeDescendantAdded(string ParentId, string ChildId, string Name);
