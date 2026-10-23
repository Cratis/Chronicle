// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Adds a child from the child's own event source to a root.
/// </summary>
/// <param name="RootId">The parent root.</param>
/// <param name="ChildId">The child identity.</param>
/// <param name="Name">The initial name.</param>
[EventType]
public record ReplayShapeChildAdded(string RootId, string ChildId, string Name);
