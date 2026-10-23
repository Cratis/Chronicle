// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Removes a leaf child by identity from its parent.
/// </summary>
/// <param name="ParentId">The immediate parent.</param>
/// <param name="ChildId">The child identity.</param>
[EventType]
public record ReplayShapeChildRemoved(string ParentId, string ChildId);
