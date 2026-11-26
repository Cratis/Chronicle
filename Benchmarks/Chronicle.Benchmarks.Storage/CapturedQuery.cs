// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>A SELECT and its parameter snapshot, copied before execution.</summary>
/// <param name="Sql">The generated SQL.</param>
/// <param name="Parameters">The captured parameters.</param>
internal sealed record CapturedQuery(string Sql, IReadOnlyList<CapturedParameter> Parameters);
