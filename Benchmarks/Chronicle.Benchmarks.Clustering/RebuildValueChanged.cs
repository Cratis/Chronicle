// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Records a monotonically increasing benchmark revision for one source.
/// </summary>
/// <param name="CatalogId">The joined catalog source.</param>
/// <param name="Revision">The new revision.</param>
[EventType]
public record RebuildValueChanged(string CatalogId, int Revision);
