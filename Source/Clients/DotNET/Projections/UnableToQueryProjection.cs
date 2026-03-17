// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Exception that gets thrown when a projection query fails.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="UnableToQueryProjection"/> class.
/// </remarks>
/// <param name="errors">The collection of error messages describing why the query failed.</param>
public class UnableToQueryProjection(IEnumerable<string> errors)
    : Exception($"Unable to query projection. Errors:\n{string.Join('\n', errors)}");

