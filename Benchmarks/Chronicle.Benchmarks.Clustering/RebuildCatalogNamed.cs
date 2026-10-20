// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Names the shared catalog joined into the benchmark read models.
/// </summary>
/// <param name="Category">The catalog name.</param>
[EventType]
public record RebuildCatalogNamed(string Category);
