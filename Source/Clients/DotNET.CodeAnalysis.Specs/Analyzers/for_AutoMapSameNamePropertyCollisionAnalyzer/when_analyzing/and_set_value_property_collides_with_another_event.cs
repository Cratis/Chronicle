// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_AutoMapSameNamePropertyCollisionAnalyzer.when_analyzing;

/// <summary>
/// The explicit setter is not limited to <c>[SetFrom]</c>: a property set with <c>[SetValue]</c> is equally at
/// risk of being overwritten by AutoMap from another event that carries the same-named property.
/// </summary>
public class and_set_value_property_collides_with_another_event : given.an_auto_map_same_name_property_collision_analyzer
{
    const string Usage = """
    public record Opened(Guid Id, string Status);
    public record Renamed(Guid Id, string Status);

    [FromEvent<Opened>]
    [FromEvent<Renamed>]
    public record Account(
        Guid Id,
        {|#0:[SetValue<Opened>("active")] string Status|});
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.AutoMapSameNamePropertyCollisionAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.AutoMapSameNamePropertyCollision, DiagnosticSeverity.Info, "Status", "Renamed"));

    [Fact] Task should_report_the_diagnostic() => _result;
}
