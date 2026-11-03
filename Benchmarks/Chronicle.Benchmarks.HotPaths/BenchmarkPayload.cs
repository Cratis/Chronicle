// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.HotPaths;

/// <summary>
/// Event content the converter benchmark deserializes into.
/// </summary>
public class BenchmarkPayload
{
    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the number.</summary>
    public int Number { get; set; }
}
