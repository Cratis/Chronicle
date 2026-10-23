// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Renames a leaf child from its own event source.
/// </summary>
/// <param name="ParentId">The immediate parent child.</param>
/// <param name="ChildId">The leaf identity.</param>
/// <param name="NewName">The new leaf name.</param>
[EventType]
public record ReplayShapeDescendantRenamed(string ParentId, string ChildId, string NewName);
