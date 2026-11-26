// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>Optional SQL query-plan support, separate from measured storage operations.</summary>
internal interface IQueryPlanHost
{
    /// <summary>Gets the labelled query capture.</summary>
    QueryCapture Capture { get; }

    /// <summary>Explains a captured query on a fresh connection.</summary>
    /// <param name="query">The captured SELECT.</param>
    /// <param name="rawPlanPath">Optional destination for the raw SQL Server showplan XML.</param>
    /// <returns>The plan, or a capture failure description.</returns>
    Task<string> Explain(CapturedQuery query, string? rawPlanPath = null);

    /// <summary>Reads index definitions for the table in a captured query.</summary>
    /// <param name="query">The captured SELECT.</param>
    /// <returns>The index definitions, or a capture failure description.</returns>
    Task<string> IndexDefinitions(CapturedQuery query);
}
