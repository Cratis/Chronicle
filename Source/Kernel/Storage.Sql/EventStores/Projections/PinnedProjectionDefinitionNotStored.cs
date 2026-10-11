// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Projections;

/// <summary>
/// The exception that is thrown when a pinned projection has no generation-exact stored definition.
/// </summary>
/// <param name="projection">The projection identifier.</param>
public class PinnedProjectionDefinitionNotStored(ProjectionId projection) : Exception($"Pinned projection '{projection}' has no generation-exact stored definition.");
