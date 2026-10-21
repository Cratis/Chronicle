// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// One independently updated key and its joined catalog value.
/// </summary>
/// <param name="Id">The source key.</param>
/// <param name="CatalogId">The joined catalog source.</param>
/// <param name="Revision">The last value appended to this key.</param>
/// <param name="Category">The joined catalog name.</param>
public record LiveRebuildReadModel(string Id, string CatalogId, int Revision, string Category);
