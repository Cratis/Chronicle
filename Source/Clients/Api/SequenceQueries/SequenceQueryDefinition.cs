// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.SequenceQueries;

/// <summary>
/// Represents the definition of a saved sequence query.
/// </summary>
/// <param name="Id">The unique identifier of the query.</param>
/// <param name="Name">The display name of the query.</param>
/// <param name="Filter">The filter definition for the query.</param>
public record SequenceQueryDefinition(
    string Id,
    string Name,
    SequenceQueryFilter Filter);
