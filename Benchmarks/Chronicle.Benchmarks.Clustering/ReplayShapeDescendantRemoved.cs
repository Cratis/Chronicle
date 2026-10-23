// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Removes a leaf child from its own event source.
/// </summary>
/// <param name="ParentId">The immediate parent child.</param>
/// <param name="ChildId">The leaf identity.</param>
[EventType]
public record ReplayShapeDescendantRemoved(string ParentId, string ChildId);
