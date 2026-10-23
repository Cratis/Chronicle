// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Creates one synthetic root on its own event source.
/// </summary>
/// <param name="Name">The root name.</param>
[EventType]
public record ReplayShapeRootCreated(string Name);
