// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// An indexed nested parent to conditionally unset before writing a leaf beneath it.
/// </summary>
/// <param name="Path">The mapped MongoDB path to the parent.</param>
/// <param name="ArrayFilters">The element filters, with the innermost filter restricted to null parents.</param>
internal record NullArrayParent(string Path, IReadOnlyList<BsonDocumentArrayFilterDefinition<BsonDocument>> ArrayFilters);
