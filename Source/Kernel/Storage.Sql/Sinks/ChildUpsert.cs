// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Describes the upsert of a child into a collection nested in other collections.
/// </summary>
/// <param name="Segments">The segments of the path to the children collection.</param>
/// <param name="Indexers">The <see cref="ArrayIndexers"/> locating the parents.</param>
/// <param name="Child">The <see cref="ChildValue"/> to upsert.</param>
internal sealed record ChildUpsert(IPropertyPathSegment[] Segments, ArrayIndexers Indexers, ChildValue Child);
