// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents the result of querying a projection against the event log.
/// </summary>
/// <param name="ReadModelEntries">Collection of JSON representations of the resulting read model entries.</param>
public record ProjectionQueryResult(IReadOnlyList<string> ReadModelEntries);

