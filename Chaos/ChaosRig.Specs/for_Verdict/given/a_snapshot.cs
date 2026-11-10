// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;

namespace Cratis.Chronicle.Chaos.for_Verdict.given;

/// <summary>
/// Provides a complete acknowledged event and its settled consumers.
/// </summary>
public class a_snapshot : Specification
{
    /// <summary>
    /// Gets a settled snapshot with one acknowledged contribution.
    /// </summary>
    private protected VerdictInput Input { get; } = new(
        [new("one", "source", 7, "acked")],
        [new("one", "source", 0, 7)],
        [new("observer", 0, true)],
        ["observer"],
        0,
        0,
        0,
        [new("source", 1, 7)],
        [new("source", 1, 7)],
        ["one"],
        1);
}
