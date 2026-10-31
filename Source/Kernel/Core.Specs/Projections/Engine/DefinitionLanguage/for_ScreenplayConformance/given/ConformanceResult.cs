// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ScreenplayConformance.given;

/// <summary>
/// The result of compiling one case both ways.
/// </summary>
/// <param name="Outcome">How the two lowerings compare.</param>
/// <param name="ChronicleErrors">The errors Chronicle reported.</param>
/// <param name="ScreenplayErrors">The errors Screenplay's semantic model reported.</param>
/// <param name="ChronicleTree">Chronicle's lowering, rendered neutrally; empty when either side rejected the case.</param>
/// <param name="ScreenplayTree">Screenplay's lowering, rendered neutrally; empty when either side rejected the case.</param>
public record ConformanceResult(
    ConformanceOutcome Outcome,
    IReadOnlyList<string> ChronicleErrors,
    IReadOnlyList<string> ScreenplayErrors,
    string ChronicleTree,
    string ScreenplayTree)
{
    /// <summary>
    /// Describes the result for a failure message.
    /// </summary>
    /// <returns>The description.</returns>
    public string Describe() =>
        $"""
        Outcome: {Outcome}
        Chronicle errors: {string.Join("; ", ChronicleErrors)}
        Screenplay errors: {string.Join("; ", ScreenplayErrors)}
        Chronicle:
        {ChronicleTree}
        Screenplay:
        {ScreenplayTree}
        """;
}
